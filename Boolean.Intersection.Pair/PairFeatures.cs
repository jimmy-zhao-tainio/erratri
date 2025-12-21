using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates;
using Geometry.Predicates.Internal;
using Topology;

namespace Boolean;

// This file describes *local* intersection data for ONE triangle pair
// (one triangle from mesh A and one from mesh B).
//
// The idea:
//
//   IntersectionSet: Tells us "triangle i in A intersects triangle j in B"
//   PairFeatures: For that (i, j) pair, stores the actual intersection vertices and line segments on those two triangles
//   IntersectionGraph: Later will glue all PairFeatures together into a global graph over the whole mesh
//
// Everything here is purely local to a single pair. No global vertex IDs are
// assigned yet; that happens in IntersectionGraph when we merge pair data.

// One intersection point for this triangle pair.
//
// The same geometric point is stored twice:
//   - OnTriangleA: barycentric coords relative to triangle A
//   - OnTriangleB: barycentric coords relative to triangle B
//
// VertexId is a pair-local index assigned by the factory; the global
// IntersectionGraph uses it to map these local vertices onto a shared
// set of global intersection vertices.
public readonly struct PairVertex
{
    public IntersectionVertexId VertexId { get; }
    public Barycentric OnTriangleA { get; }
    public Barycentric OnTriangleB { get; }

    public PairVertex(IntersectionVertexId vertexId,
                      Barycentric onTriangleA,
                      Barycentric onTriangleB)
    {
        VertexId = vertexId;
        OnTriangleA = onTriangleA;
        OnTriangleB = onTriangleB;
    }
}

// Simple undirected edge between two PairVertex instances.
//
// Start/End are the PairVertex instances for this segment; they correspond
// to entries in the PairFeatures.Vertices list. The same segment lives on
// both triangles A and B; which triangle you care about is handled when we
// later convert barycentric coords to world positions.
public readonly struct PairSegment
{
    public PairVertex Start { get; }
    public PairVertex End { get; }

    public PairSegment(PairVertex start, PairVertex end)
    {
        Start = start;
        End = end;
    }
}

// All intersection geometry for a single triangle pair.
//
// For one (triangleA, triangleB) pair we collect:
//   - All intersection vertices (Vertices)
//   - All intersection line segments between those vertices (Segments)
//
// Intersections.Type tells us if this pair meets at:
//   - a single point
//   - a segment
//   - or an overlapping area (coplanar case)
//
// This struct does NOT compute anything by itself; it just stores the result
// of running the low-level math in PairIntersectionMath.
public sealed class PairFeatures
{
    public IntersectionSet.Intersection Intersection { get; }
    public IReadOnlyList<PairVertex> Vertices { get; }
    public IReadOnlyList<PairSegment> Segments { get; }

    public PairFeatures(IntersectionSet.Intersection intersection,
                        IReadOnlyList<PairVertex> vertices,
                        IReadOnlyList<PairSegment> segments)
    {
        Intersection = intersection;
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
    }
}

// Factory for building PairFeatures.
//
//   - Looks up the actual Triangle A + Triangle B for this pair
//   - Asks PairIntersectionMath for all raw intersection points
//   - Converts those to barycentric coords on both triangles
//   - Builds a clean list of PairVertex + PairSegment for this pair
//
// That way, all the "how do we compute intersections?" logic is in one place.
public static class PairFeaturesFactory
{
    public static PairFeatures CreateEmpty(in IntersectionSet.Intersection intersection)
    {
        var vertices = Array.Empty<PairVertex>();
        var segments = Array.Empty<PairSegment>();
        return new PairFeatures(intersection, vertices, segments);
    }

    // Design notes for PairFeaturesFactory.Create:
    //
    // - Work per pair only: we take one triangle from mesh A and one from mesh B,
    //   look at their IntersectionType, and build a local set of barycentric
    //   vertices (PairVertex) and segments (PairSegment) that describe how those
    //   two triangles touch.
    //
    // - Low-level geometry (plane intersections, coplanar 2D projection, and
    //   barycentric solvers) lives in Geometry.Internal.PairIntersectionMath.
    //   This factory delegates to those helpers to obtain intersection samples
    //   and then:
    //     * converts them to barycentric coordinates on A and B,
    //     * deduplicates vertices using feature-level tolerances,
    //     * degrades noisy inputs so the output is consistent with the
    //       reported IntersectionType without reclassifying it.
    public static PairFeatures Create(in IntersectionSet set, in IntersectionSet.Intersection intersection)
    {
        if (set.TrianglesA is null) throw new ArgumentNullException(nameof(set.TrianglesA));
        if (set.TrianglesB is null) throw new ArgumentNullException(nameof(set.TrianglesB));

        var triangleA = set.TrianglesA[intersection.TriangleIndexA];
        var triangleB = set.TrianglesB[intersection.TriangleIndexB];

        var vertices = new List<PairVertex>();
        var segments = new List<PairSegment>();

        bool coplanar = TrianglePredicates.IsCoplanar(in triangleA, in triangleB);

        if (coplanar)
            BuildCoplanarFeatures(in triangleA, in triangleB, intersection.Type, vertices, segments);
        else
            BuildNonCoplanarFeatures(in triangleA, in triangleB, intersection.Type, vertices, segments);

        return new PairFeatures(intersection, vertices, segments);
    }

    private static void BuildNonCoplanarFeatures(in Triangle triangleA,
                                                 in Triangle triangleB,
                                                 IntersectionType type,
                                                 List<PairVertex> vertices,
                                                 List<PairSegment> segments)
    {
        var rawPoints = PairIntersectionMath.ComputeNonCoplanarIntersectionPoints(in triangleA, in triangleB);

        if (rawPoints.Count == 0)
        {
            if (type != IntersectionType.None)
                System.Diagnostics.Debug.Assert(false, "Non-empty intersection type but no non-coplanar feature vertices were found.");
            return;
        }

        // Apply an additional feature-layer dedup in world space so
        // downstream barycentric merging operates on a stable set of
        // samples.
        var uniquePoints = new List<RealPoint>(rawPoints.Count);
        foreach (var v in rawPoints)
        {
            var p = new RealPoint(v.X, v.Y, v.Z);
            AddUniqueWorldPoint(uniquePoints, in p);
        }

        if (uniquePoints.Count == 0)
        {
            if (type != IntersectionType.None)
                System.Diagnostics.Debug.Assert(false, "Non-empty intersection type but no non-coplanar feature vertices were found after dedup.");
            return;
        }

        var barycentricVertices = new BarycentricVertices();
        var realTriangleA = new RealTriangle(triangleA);
        var realTriangleB = new RealTriangle(triangleB);

        for (int i = 0; i < uniquePoints.Count; i++)
        {
            RealPoint p = uniquePoints[i];
            var barycentricA = realTriangleA.ComputeBarycentric(in p, out double denominatorA);
            if (denominatorA == 0.0)
            {
                System.Diagnostics.Debug.Assert(false, "Degenerate triangle in ToBarycentric.");
                barycentricA = new Barycentric(0.0, 0.0, 0.0);
            }

            var barycentricB = realTriangleB.ComputeBarycentric(in p, out double denominatorB);
            if (denominatorB == 0.0)
            {
                System.Diagnostics.Debug.Assert(false, "Degenerate triangle in ToBarycentric.");
                barycentricB = new Barycentric(0.0, 0.0, 0.0);
            }

            int idx = AddOrGetVertex(vertices, barycentricA, barycentricB);
            barycentricVertices.Add(idx, in p);
        }

        if (vertices.Count == 0)
        {
            if (type != IntersectionType.None)
                System.Diagnostics.Debug.Assert(false, "Non-empty intersection type but no non-coplanar feature vertices were created.");
            return;
        }

        // Degrade geometry to remain consistent with the reported
        // IntersectionType without reclassifying it.
        if (type == IntersectionType.Point)
        {
            // Classifier says "point", but numerics may have produced
            // multiple nearby samples. Keep a single representative
            // vertex and drop any segments.
            if (vertices.Count == 0)
            {
                System.Diagnostics.Debug.Assert(false, "IntersectionType.Point but no feature vertices were found.");
                return;
            }

            var v0 = vertices[0];
            vertices.Clear();
            vertices.Add(v0);
            segments.Clear();
            return;
        }

        if (type == IntersectionType.Segment || type == IntersectionType.Area)
        {
            // Non-coplanar triangles should not report Area, but if they
            // do we treat it as at most a segment.
            if (vertices.Count < 2)
            {
                // Segment collapsed to a point; keep at most one vertex
                // and emit no segments.
                if (vertices.Count > 1)
                {
                    var v0 = vertices[0];
                    vertices.Clear();
                    vertices.Add(v0);
                }
                segments.Clear();
                return;
            }

            // For non-coplanar intersections, ensure any intermediate points are
            // preserved as a chain so adjacent triangles cannot disagree on
            // whether a segment is split by a shared vertex.
            segments.Clear();
            BuildCollinearChainSegments(vertices, barycentricVertices.Samples, segments);
            return;
        }
    }

    private static void BuildCollinearChainSegments(
        IReadOnlyList<PairVertex> vertices,
        IReadOnlyList<PairVertexSample3D> samples,
        List<PairSegment> segments)
    {
        if (samples.Count < 2)
        {
            return;
        }

        PairVertexSample3D.FindFarthestPair(samples, out int startVertexIndex, out int endVertexIndex);
        if (startVertexIndex == endVertexIndex)
        {
            return;
        }

        bool foundStart = false;
        bool foundEnd = false;
        var startPoint = samples[0].Point;
        var endPoint = samples[0].Point;
        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].VertexIndex == startVertexIndex)
            {
                startPoint = samples[i].Point;
                foundStart = true;
            }
            if (samples[i].VertexIndex == endVertexIndex)
            {
                endPoint = samples[i].Point;
                foundEnd = true;
            }
        }

        if (!foundStart || !foundEnd)
        {
            return;
        }

        var axis = RealVector.FromPoints(in startPoint, in endPoint);
        double axisLenSq = axis.Dot(in axis);
        if (axisLenSq <= 0.0)
        {
            return;
        }

        var ordered = new List<(double T, int VertexIndex)>(samples.Count);
        var seen = new HashSet<int>();

        for (int i = 0; i < samples.Count; i++)
        {
            int vid = samples[i].VertexIndex;
            if (!seen.Add(vid))
            {
                continue;
            }

            var p = samples[i].Point;
            var ap = RealVector.FromPoints(in startPoint, in p);
            double t = ap.Dot(in axis) / axisLenSq;
            ordered.Add((t, vid));
        }

        ordered.Sort(static (a, b) => a.T.CompareTo(b.T));

        int prev = ordered[0].VertexIndex;
        for (int i = 1; i < ordered.Count; i++)
        {
            int next = ordered[i].VertexIndex;
            if (prev == next)
            {
                continue;
            }

            segments.Add(new PairSegment(vertices[prev], vertices[next]));
            prev = next;
        }
    }

    private static void BuildCoplanarFeatures(in Triangle triangleA,
                                              in Triangle triangleB,
                                              IntersectionType type,
                                              List<PairVertex> vertices,
                                              List<PairSegment> segments)
    {
        var candidates = PairIntersectionMath.ComputeCoplanarIntersectionPoints(in triangleA, in triangleB);

        if (candidates.Count == 0)
        {
            if (type != IntersectionType.None)
                System.Diagnostics.Debug.Assert(false, "Non-empty intersection type but no coplanar feature vertices were found.");
            return;
        }

        // Map 2D intersection samples to barycentric coordinates on A and B.
        var plane = TriangleProjection2D.ChooseProjectionAxis(triangleA.Normal);
        TriangleProjection2D.ProjectTriangleTo2D(in triangleA, plane, out var a0, out var a1, out var a2);
        TriangleProjection2D.ProjectTriangleTo2D(in triangleB, plane, out var b0, out var b1, out var b2);

        var barycentricVertices2D = new BarycentricVertices2D();
        for (int i = 0; i < candidates.Count; i++)
        {
            var p = candidates[i];
            var p2d = new TriangleProjection2D.Point2D(p.X, p.Y);
            var barycentricA = TriangleProjection2D.ToBarycentric2D(in p2d, in a0, in a1, in a2);
            var barycentricB = TriangleProjection2D.ToBarycentric2D(in p2d, in b0, in b1, in b2);

            int idx = AddOrGetVertex(vertices, barycentricA, barycentricB);
            barycentricVertices2D.Add(idx, in p);
        }

        if (vertices.Count == 0)
        {
            if (type != IntersectionType.None)
                System.Diagnostics.Debug.Assert(false, "Non-empty intersection type but no coplanar feature vertices were created.");
            return;
        }

        // Degrade geometry according to the reported IntersectionType.
        if (type == IntersectionType.Point)
        {
            if (vertices.Count > 1)
            {
                // Classifier says "point" but we sampled multiple
                // nearby locations; keep a single representative.
                var v0 = vertices[0];
                vertices.Clear();
                vertices.Add(v0);
            }
            segments.Clear();
            return;
        }

        if (type == IntersectionType.Segment)
        {
            if (vertices.Count < 2)
            {
                // Segment collapsed to a point; keep at most one
                // vertex and emit no segments.
                if (vertices.Count > 1)
                {
                    var v0 = vertices[0];
                    vertices.Clear();
                    vertices.Add(v0);
                }
                segments.Clear();
                return;
            }

            // Find the two farthest 2D samples and connect them.
            barycentricVertices2D.FindFarthestPair(out int startIndex, out int endIndex);

            segments.Clear();
            if (startIndex != endIndex)
                segments.Add(new PairSegment(vertices[startIndex], vertices[endIndex]));
            return;
        }

        if (type == IntersectionType.Area)
        {
            // Area intersection: build a convex boundary loop from all samples.
            var orderedVertexIndices = barycentricVertices2D.BuildOrderedUniqueLoop();
            int uniqueCount = orderedVertexIndices.Count;
            if (uniqueCount < 3)
            {
                // Area collapsed to a lower-dimensional feature.
                if (uniqueCount == 2)
                {
                    segments.Clear();
                    segments.Add(new PairSegment(vertices[orderedVertexIndices[0]], vertices[orderedVertexIndices[1]]));
                }
                else
                {
                    // Single point (or none); represent as point only.
                    segments.Clear();
                }
                return;
            }

            segments.Clear();
            for (int i = 0; i < uniqueCount; i++)
            {
                int current = orderedVertexIndices[i];
                int next = orderedVertexIndices[(i + 1) % uniqueCount];
                if (current != next)
                    segments.Add(new PairSegment(vertices[current], vertices[next]));
            }
            return;
        }
    }

    // Two vertices are considered identical only if both their
    // barycentric coordinates on A and on B are close within
    // BarycentricEpsilon. This keeps the local vertex set stable
    // even if world-space computations produce slightly different
    // samples for the same geometric point.
    private static int AddOrGetVertex(List<PairVertex> vertices,
                                      Barycentric onA,
                                      Barycentric onB)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            if (v.OnTriangleA.IsCloseTo(in onA) && v.OnTriangleB.IsCloseTo(in onB))
                return i;
        }

        var id = new IntersectionVertexId(vertices.Count);
        var vertex = new PairVertex(id, onA, onB);
        vertices.Add(vertex);
        return vertices.Count - 1;
    }

    private static void AddUniqueWorldPoint(List<RealPoint> points, in RealPoint candidate)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var existing = points[i];
            double squaredDistance = existing.DistanceSquared(in candidate);

            if (squaredDistance <= Tolerances.FeatureWorldDistanceEpsilonSquared)
                return;
        }
        points.Add(candidate);
    }
}
