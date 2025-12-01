using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates;

namespace Kernel.Pslg.Phases;

// Phase #1: build an initial PSLG for one triangle and its intersection points/segments.
// Vertices:
//   - 0: corner V0 -> (1, 0)
//   - 1: corner V1 -> (0, 1)
//   - 2: corner V2 -> (0, 0)
//   - 3..: intersection points in the same order as the input list, mapped to (u, v).
// Edges:
//   - Boundary edges: (0,1), (1,2), (2,0) marked IsBoundary = true.
//   - Segment edges: for each IntersectionSegment (i,j), add an edge between (3 + i) and (3 + j).
// This is intentionally minimal and does not yet split boundary edges or build half-edge/face structures.
internal static class PslgBuildPhase
{
    internal static PslgBuildState Run(in PslgInput input)
    {
        if (input.Points is null) throw new ArgumentNullException(nameof(input.Points));
        if (input.Segments is null) throw new ArgumentNullException(nameof(input.Segments));

        var points = input.Points;
        var segments = input.Segments;

        var vertices = new List<PslgVertex>(capacity: 3 + points.Count);

        // Triangle corners in barycentric (u,v) chart.
        vertices.Add(new PslgVertex(1.0, 0.0, isTriangleCorner: true, cornerIndex: 0)); // V0
        vertices.Add(new PslgVertex(0.0, 1.0, isTriangleCorner: true, cornerIndex: 1)); // V1
        vertices.Add(new PslgVertex(0.0, 0.0, isTriangleCorner: true, cornerIndex: 2)); // V2

        // Intersection points.
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            double u = p.Barycentric.U;
            double v = p.Barycentric.V;
            vertices.Add(new PslgVertex(u, v, isTriangleCorner: false, cornerIndex: -1));
        }

        // Normalize vertices (clamp, snap, deduplicate) and keep a mapping from original indices to representatives.
        var indexMap = NormalizeVertices(vertices);

        // Build boundary edges, intersection edges, and verify that there are no crossings without explicit vertices.
        var edges = new List<PslgEdge>(capacity: 3 + segments.Count);
        var edgeKeys = new HashSet<(int, int)>();

        BuildBoundaryEdges(vertices, edges, edgeKeys);
        BuildIntersectionEdges(points, segments, indexMap, vertices, edges, edgeKeys);
        VerifyNoCrossings(vertices, edges);

        return new PslgBuildState(vertices, edges);
    }

    // Snap and deduplicate PSLG vertices.
    //
    // - Clamp (u,v) into the reference triangle domain:
    //       u >= 0, v >= 0, u + v <= 1.
    // - Snap vertices that are within EpsCorner of a triangle corner onto that
    //   corner exactly.
    // - Merge vertices whose distance is within EpsVertex, keeping a single
    //   representative. Returns an index map from original indices to
    //   representative vertex indices.
    private static int[] NormalizeVertices(List<PslgVertex> vertices)
    {
        if (vertices.Count == 0)
        {
            return Array.Empty<int>();
        }

        var original = vertices.ToArray();
        var newVertices = new List<PslgVertex>(original.Length);
        var indexMap = new int[original.Length];

        var corner0 = original[0];
        var corner1 = original[1];
        var corner2 = original[2];

        for (int i = 0; i < original.Length; i++)
        {
            var vOrig = original[i];
            double x = vOrig.X;
            double y = vOrig.Y;

            bool isCorner = vOrig.IsTriangleCorner;
            int cornerIndex = vOrig.CornerIndex;

            if (!isCorner)
            {
                (x, y) = ClampToReferenceTriangle(x, y);

                int snappedCorner = SnapToCorner(x, y, corner0, corner1, corner2);
                if (snappedCorner >= 0)
                {
                    isCorner = true;
                    cornerIndex = snappedCorner;

                    switch (snappedCorner)
                    {
                        case 0:
                            x = corner0.X;
                            y = corner0.Y;
                            break;
                        case 1:
                            x = corner1.X;
                            y = corner1.Y;
                            break;
                        case 2:
                            x = corner2.X;
                            y = corner2.Y;
                            break;
                    }
                }
            }

            int representativeIndex = -1;
            for (int j = 0; j < newVertices.Count; j++)
            {
                var existing = newVertices[j];
                double dx = x - existing.X;
                double dy = y - existing.Y;
                double dist2 = dx * dx + dy * dy;

                if (dist2 <= Tolerances.PslgVertexMergeEpsilonSquared)
                {
                    representativeIndex = j;
                    break;
                }
            }

            if (representativeIndex < 0)
            {
                newVertices.Add(new PslgVertex(x, y, isCorner, cornerIndex));
                representativeIndex = newVertices.Count - 1;
            }

            indexMap[i] = representativeIndex;
        }

        vertices.Clear();
        vertices.AddRange(newVertices);
        return indexMap;
    }

    private static (double x, double y) ClampToReferenceTriangle(double u, double v)
    {
        if (u < 0.0) u = 0.0;
        if (v < 0.0) v = 0.0;

        if (u > 1.0) u = 1.0;
        if (v > 1.0) v = 1.0;

        double sum = u + v;
        if (sum > 1.0)
        {
            double inv = 1.0 / sum;
            u *= inv;
            v *= inv;
        }

        return (u, v);
    }

    private static int SnapToCorner(
        double x,
        double y,
        PslgVertex corner0,
        PslgVertex corner1,
        PslgVertex corner2)
    {
        int bestIndex = -1;
        double bestDist2 = Tolerances.EpsCorner * Tolerances.EpsCorner;

        void Consider(int index, PslgVertex c)
        {
            double dx = x - c.X;
            double dy = y - c.Y;
            double dist2 = dx * dx + dy * dy;
            if (dist2 <= bestDist2)
            {
                bestDist2 = dist2;
                bestIndex = index;
            }
        }

        Consider(0, corner0);
        Consider(1, corner1);
        Consider(2, corner2);

        return bestIndex;
    }

    // Build triangle boundary edges, split at all vertices that lie on each side.
    // Edges are oriented along the triangle boundary cycle V0->V1->V2->V0.
    private static void BuildBoundaryEdges(
        List<PslgVertex> vertices,
        List<PslgEdge> edges,
        HashSet<(int, int)> edgeKeys)
    {
        // Side 0: V0 -> V1, with vertices satisfying u + v = 1 (within epsilon).
        BuildBoundarySide(
            vertices,
            edges,
            edgeKeys,
            static v => Math.Abs(v.X + v.Y - 1.0) <= Tolerances.EpsSide,
            static v => v.Y,
            ascending: true);

        // Side 1: V1 -> V2, with vertices satisfying u = 0.
        BuildBoundarySide(
            vertices,
            edges,
            edgeKeys,
            static v => Math.Abs(v.X) <= Tolerances.EpsSide,
            static v => v.Y,
            ascending: false);

        // Side 2: V2 -> V0, with vertices satisfying v = 0.
        BuildBoundarySide(
            vertices,
            edges,
            edgeKeys,
            static v => Math.Abs(v.Y) <= Tolerances.EpsSide,
            static v => v.X,
            ascending: true);
    }

    private static void BuildBoundarySide(
        List<PslgVertex> vertices,
        List<PslgEdge> edges,
        HashSet<(int, int)> edgeKeys,
        Func<PslgVertex, bool> isOnSide,
        Func<PslgVertex, double> param,
        bool ascending)
    {
        var indices = new List<(int index, double t)>();

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            if (isOnSide(v))
            {
                indices.Add((i, param(v)));
            }
        }

        if (indices.Count < 2)
        {
            return;
        }

        indices.Sort((a, b) => a.t.CompareTo(b.t));
        if (!ascending)
        {
            indices.Reverse();
        }

        for (int i = 0; i < indices.Count - 1; i++)
        {
            int a = indices[i].index;
            int b = indices[i + 1].index;
            if (a == b)
            {
                continue;
            }

            var key = a < b ? (a, b) : (b, a);
            if (edgeKeys.Add(key))
            {
                edges.Add(new PslgEdge(a, b, isBoundary: true));
            }
        }
    }

    // Build intersection edges between PSLG vertices corresponding to IntersectionSegment endpoints (after vertex deduplication).
    private static void BuildIntersectionEdges(
        IReadOnlyList<TriangleSubdivision.IntersectionPoint> points,
        IReadOnlyList<TriangleSubdivision.IntersectionSegment> segments,
        int[] indexMap,
        List<PslgVertex> vertices,
        List<PslgEdge> edges,
        HashSet<(int, int)> edgeKeys)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.StartIndex < 0 || seg.StartIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment start index is out of range.");
            }

            if (seg.EndIndex < 0 || seg.EndIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment end index is out of range.");
            }

            int startOriginal = 3 + seg.StartIndex;
            int endOriginal = 3 + seg.EndIndex;

            if ((uint)startOriginal >= (uint)indexMap.Length ||
                (uint)endOriginal >= (uint)indexMap.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment indices are out of range after vertex normalization.");
            }

            int start = indexMap[startOriginal];
            int end = indexMap[endOriginal];

            if (start == end)
            {
                continue;
            }

            var key = start < end ? (start, end) : (end, start);
            if (edgeKeys.Add(key))
            {
                edges.Add(new PslgEdge(start, end, isBoundary: false));
            }
        }
    }

    // Verify that no two edges cross in their interiors without an explicit PSLG vertex at the crossing.
    private static void VerifyNoCrossings(
        List<PslgVertex> vertices,
        List<PslgEdge> edges)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var e1 = edges[i];
            for (int j = i + 1; j < edges.Count; j++)
            {
                var e2 = edges[j];

                // Ignore edges that share a vertex: junctions and endpoints
                // are allowed; only pure crossings are forbidden.
                if (e1.Start == e2.Start || e1.Start == e2.End ||
                    e1.End == e2.Start || e1.End == e2.End)
                {
                    continue;
                }

                if (!RealSegmentPredicates.TryIntersect(
                        new RealSegment(
                            new RealPoint(vertices[e1.Start].X, vertices[e1.Start].Y, 0.0),
                            new RealPoint(vertices[e1.End].X, vertices[e1.End].Y, 0.0)),
                        new RealSegment(
                            new RealPoint(vertices[e2.Start].X, vertices[e2.Start].Y, 0.0),
                            new RealPoint(vertices[e2.End].X, vertices[e2.End].Y, 0.0)),
                        out var intersection))
                {
                    continue;
                }

                if (!IsNearExistingVertex(vertices, intersection.X, intersection.Y))
                {
                    throw new InvalidOperationException("PSLG requires no crossings without vertices.");
                }
            }
        }
    }

    private static bool IsNearExistingVertex(
        List<PslgVertex> vertices,
        double x,
        double y)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            double dx = x - v.X;
            double dy = y - v.Y;
            double dist2 = dx * dx + dy * dy;
            if (dist2 <= Tolerances.PslgVertexMergeEpsilonSquared)
            {
                return true;
            }
        }

        return false;
    }
}
