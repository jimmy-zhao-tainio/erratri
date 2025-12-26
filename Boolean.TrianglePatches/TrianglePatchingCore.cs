using System;
using System.Collections.Generic;
using System.IO;
using Geometry;
using Boolean.Intersection.Indexing;
using Boolean.Intersection.Topology;

namespace Boolean;

internal static class TrianglePatchingCore
{
    private static bool s_dumpWritten = false;

    internal static TrianglePatches Run(
        IntersectionGraph graph,
        IntersectionIndex index,
        MeshA topologyA,
        MeshB topologyB)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (index is null) throw new ArgumentNullException(nameof(index));
        if (topologyA is null) throw new ArgumentNullException(nameof(topologyA));
        if (topologyB is null) throw new ArgumentNullException(nameof(topologyB));

        var trianglesA = graph.IntersectionSet.TrianglesA
            ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesA));
        var trianglesB = graph.IntersectionSet.TrianglesB
            ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesB));

        var edgeLookup = BuildEdgeLookup(graph);

        var patchesA = BuildMeshPatches(
            trianglesA,
            index.TrianglesA,
            topologyA.TriangleEdges,
            edgeLookup);

        var patchesB = BuildMeshPatches(
            trianglesB,
            index.TrianglesB,
            topologyB.TriangleEdges,
            edgeLookup);

        return new TrianglePatches(patchesA, patchesB);
    }

    private static Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> BuildEdgeLookup(IntersectionGraph graph)
    {
        var lookup = new Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)>();
        foreach (var (id, start, end) in graph.Edges)
        {
            lookup[id.Value] = (start, end);
        }
        return lookup;
    }

    private static IReadOnlyList<IReadOnlyList<RealTriangle>> BuildMeshPatches(
        IReadOnlyList<Triangle> triangles,
        IReadOnlyList<TriangleIntersectionVertex[]> triangleVertices,
        IReadOnlyList<IntersectionEdgeId[]> triangleEdges,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeLookup)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        if (triangleVertices is null) throw new ArgumentNullException(nameof(triangleVertices));
        if (triangleEdges is null) throw new ArgumentNullException(nameof(triangleEdges));
        if (edgeLookup is null) throw new ArgumentNullException(nameof(edgeLookup));

        if (triangles.Count != triangleVertices.Count || triangles.Count != triangleEdges.Count)
        {
            throw new InvalidOperationException("Triangle counts do not match intersection index/topology.");
        }

        var result = new IReadOnlyList<RealTriangle>[triangles.Count];

        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            var vertices = triangleVertices[i];
            var edges = triangleEdges[i];

            var points = new List<Triangulation.IntersectionPoint>(vertices.Length);
            var pointIndexByVertexId = new Dictionary<int, int>(vertices.Length);

            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                var vertex = vertices[vertexIndex];
                var barycentric = vertex.Barycentric;
                var world = Barycentric.ToRealPointOnTriangle(in triangle, in barycentric);
                pointIndexByVertexId[vertex.VertexId.Value] = points.Count;
                points.Add(new Triangulation.IntersectionPoint(vertex.Barycentric, world));
            }

            var segments = new List<Triangulation.IntersectionSegment>(edges.Length);
            var seenSegments = new HashSet<(int, int)>();

            for (int e = 0; e < edges.Length; e++)
            {
                var edgeId = edges[e];
                if (!edgeLookup.TryGetValue(edgeId.Value, out var endpoints))
                {
                    throw new InvalidOperationException($"Edge {edgeId.Value} not found in intersection graph.");
                }

                if (!pointIndexByVertexId.TryGetValue(endpoints.Start.Value, out var startIdx) ||
                    !pointIndexByVertexId.TryGetValue(endpoints.End.Value, out var endIdx))
                {
                    throw new InvalidOperationException("Triangle edge references a vertex not present on the triangle.");
                }

                if (startIdx == endIdx)
                {
                    continue; // degenerate segment
                }

                var key = Normalize(startIdx, endIdx);
                if (!seenSegments.Add(key))
                {
                    continue; // dedup identical segments
                }

                segments.Add(new Triangulation.IntersectionSegment(startIdx, endIdx));
            }

            segments = SplitSegmentsPassingThroughPoints(points, segments);

#if DEBUG
            DebugAssertNoSegmentPassesThroughPoint(i, points, segments);
#endif

            IReadOnlyList<RealTriangle> patches;
            try
            {
                patches = Triangulation.Subdivide(in triangle, points, segments);
            }
            catch (Exception ex)
            {
                TryDumpFailure(i, triangle, points, segments, ex);
                throw;
            }

            var stored = patches is List<RealTriangle> list ? list : new List<RealTriangle>(patches);
            result[i] = stored.ToArray();
        }

        return result;
    }

    private static (int, int) Normalize(int a, int b) => a < b ? (a, b) : (b, a);

    private static List<Triangulation.IntersectionSegment> SplitSegmentsPassingThroughPoints(
        IReadOnlyList<Triangulation.IntersectionPoint> points,
        IReadOnlyList<Triangulation.IntersectionSegment> segments)
    {
        if (segments.Count == 0 || points.Count < 3)
        {
            return segments as List<Triangulation.IntersectionSegment>
                ?? new List<Triangulation.IntersectionSegment>(segments);
        }

        double interiorTEpsilon = Tolerances.FeatureBarycentricEpsilon;
        double mergeDistanceEpsilon = 10.0 * Tolerances.MergeEpsilon;
        double mergeDistanceEpsilonSquared = mergeDistanceEpsilon * mergeDistanceEpsilon;

        var output = new List<Triangulation.IntersectionSegment>(segments.Count);
        var seenSegments = new HashSet<(int, int)>();
        var interiorPoints = new List<(double T, int Index)>();

        for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            var segment = segments[segmentIndex];
            int startIndex = segment.StartIndex;
            int endIndex = segment.EndIndex;

            if ((uint)startIndex >= (uint)points.Count ||
                (uint)endIndex >= (uint)points.Count ||
                startIndex == endIndex)
            {
                continue;
            }

            if (!TryCollectInteriorPointsOnSegment(
                    points,
                    startIndex,
                    endIndex,
                    interiorTEpsilon,
                    mergeDistanceEpsilonSquared,
                    interiorPoints))
            {
                AddSegmentDedup(output, seenSegments, startIndex, endIndex);
                continue;
            }

            interiorPoints.Sort(static (a, b) => a.T.CompareTo(b.T));

            int previous = startIndex;
            for (int i = 0; i < interiorPoints.Count; i++)
            {
                int next = interiorPoints[i].Index;
                AddSegmentDedup(output, seenSegments, previous, next);
                previous = next;
            }

            AddSegmentDedup(output, seenSegments, previous, endIndex);
        }

        return output;
    }

    private static bool TryCollectInteriorPointsOnSegment(
        IReadOnlyList<Triangulation.IntersectionPoint> points,
        int startIndex,
        int endIndex,
        double interiorTEpsilon,
        double mergeDistanceEpsilonSquared,
        List<(double T, int Index)> interiorPoints)
    {
        interiorPoints.Clear();

        var start = points[startIndex].Position;
        var end = points[endIndex].Position;
        var segmentDirection = RealVector.FromPoints(in start, in end);
        double segmentLengthSquared = segmentDirection.Dot(in segmentDirection);

        if (segmentLengthSquared <= 0.0)
        {
            return false;
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (i == startIndex || i == endIndex)
            {
                continue;
            }

            var p = points[i].Position;
            var startToPoint = RealVector.FromPoints(in start, in p);
            double t = startToPoint.Dot(in segmentDirection) / segmentLengthSquared;

            if (t <= interiorTEpsilon || t >= 1.0 - interiorTEpsilon)
            {
                continue;
            }

            var closest = LinearInterpolation(in start, in end, t);
            if (p.DistanceSquared(in closest) > mergeDistanceEpsilonSquared)
            {
                continue;
            }

            interiorPoints.Add((t, i));
        }

        return interiorPoints.Count > 0;
    }

    private static RealPoint LinearInterpolation(in RealPoint a, in RealPoint b, double t)
    {
        return new RealPoint(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
    }

    private static void AddSegmentDedup(
        List<Triangulation.IntersectionSegment> segments,
        HashSet<(int, int)> seenSegments,
        int a,
        int b)
    {
        if (a == b)
        {
            return;
        }

        var key = Normalize(a, b);
        if (!seenSegments.Add(key))
        {
            return;
        }

        segments.Add(new Triangulation.IntersectionSegment(a, b));
    }

#if DEBUG
    private static void DebugAssertNoSegmentPassesThroughPoint(
        int triangleIndex,
        IReadOnlyList<Triangulation.IntersectionPoint> points,
        IReadOnlyList<Triangulation.IntersectionSegment> segments)
    {
        double interiorTEpsilon = Tolerances.FeatureBarycentricEpsilon;
        double mergeDistanceEpsilon = 10.0 * Tolerances.MergeEpsilon;
        double mergeDistanceEpsilonSquared = mergeDistanceEpsilon * mergeDistanceEpsilon;

        var interiorPoints = new List<(double T, int Index)>();
        for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            var segment = segments[segmentIndex];
            if ((uint)segment.StartIndex >= (uint)points.Count ||
                (uint)segment.EndIndex >= (uint)points.Count ||
                segment.StartIndex == segment.EndIndex)
            {
                continue;
            }

            if (TryCollectInteriorPointsOnSegment(
                    points,
                    segment.StartIndex,
                    segment.EndIndex,
                    interiorTEpsilon,
                    mergeDistanceEpsilonSquared,
                    interiorPoints))
            {
                System.Diagnostics.Debug.Fail(
                    $"TrianglePatches: segment passes through another point (tri={triangleIndex} seg={segment.StartIndex}->{segment.EndIndex} via={interiorPoints[0].Index}).");
                return;
            }
        }
    }
#endif

    private static void TryDumpFailure(
        int triangleIndex,
        Triangle triangle,
        IReadOnlyList<Triangulation.IntersectionPoint> points,
        IReadOnlyList<Triangulation.IntersectionSegment> segments,
        Exception ex)
    {
        if (s_dumpWritten)
        {
            return;
        }

        s_dumpWritten = true;
        var path = "triangle_subdivision_fail_dump.txt";

        try
        {
            using var sw = new StreamWriter(path, append: false);
            sw.WriteLine($"triangleIndex={triangleIndex}");
            sw.WriteLine($"triangle=P0=({triangle.P0.X},{triangle.P0.Y},{triangle.P0.Z}) P1=({triangle.P1.X},{triangle.P1.Y},{triangle.P1.Z}) P2=({triangle.P2.X},{triangle.P2.Y},{triangle.P2.Z})");
            sw.WriteLine("points:");

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                sw.WriteLine($"  {i}: bary=({p.Barycentric.U},{p.Barycentric.V},{p.Barycentric.W}) world=({p.Position.X},{p.Position.Y},{p.Position.Z})");
            }

            sw.WriteLine("segments:");
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                sw.WriteLine($"  {i}: {s.StartIndex} -> {s.EndIndex}");
            }

            sw.WriteLine($"exception={ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"TriangleSubdivision failure captured to {path}");
        }
        catch
        {
            // ignore secondary dump failures
        }
    }
}
