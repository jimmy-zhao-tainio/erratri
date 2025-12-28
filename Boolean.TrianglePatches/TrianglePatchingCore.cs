using System;
using System.Collections.Generic;
using System.IO;
using Geometry;
using Geometry.Predicates;
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
        var (coplanarA, coplanarB) = BuildCoplanarPairs(graph, trianglesA, trianglesB);

        var patchesA = BuildMeshPatches(
            trianglesA,
            trianglesB,
            index.TrianglesA,
            topologyA.TriangleEdges,
            edgeLookup,
            coplanarA);

        var patchesB = BuildMeshPatches(
            trianglesB,
            trianglesA,
            index.TrianglesB,
            topologyB.TriangleEdges,
            edgeLookup,
            coplanarB);

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

    private static IReadOnlyList<IReadOnlyList<TrianglePatch>> BuildMeshPatches(
        IReadOnlyList<Triangle> triangles,
        IReadOnlyList<Triangle> otherTriangles,
        IReadOnlyList<TriangleIntersectionVertex[]> triangleVertices,
        IReadOnlyList<IntersectionEdgeId[]> triangleEdges,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeLookup,
        IReadOnlyList<List<CoplanarPair>?> coplanarPairs)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        if (triangleVertices is null) throw new ArgumentNullException(nameof(triangleVertices));
        if (triangleEdges is null) throw new ArgumentNullException(nameof(triangleEdges));
        if (edgeLookup is null) throw new ArgumentNullException(nameof(edgeLookup));

        if (triangles.Count != triangleVertices.Count || triangles.Count != triangleEdges.Count)
        {
            throw new InvalidOperationException("Triangle counts do not match intersection index/topology.");
        }

        var result = new IReadOnlyList<TrianglePatch>[triangles.Count];

        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            var vertices = triangleVertices[i];
            var edges = triangleEdges[i];

            var points = new List<IntersectionPoint>(vertices.Length);
            var pointIndexByVertexId = new Dictionary<int, int>(vertices.Length);
            var pointVertexIds = new List<int>(vertices.Length);

            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                var vertex = vertices[vertexIndex];
                var barycentric = vertex.Barycentric;
                var world = Barycentric.ToRealPointOnTriangle(in triangle, in barycentric);
                pointIndexByVertexId[vertex.VertexId.Value] = points.Count;
                points.Add(new IntersectionPoint(vertex.Barycentric, world));
                pointVertexIds.Add(vertex.VertexId.Value);
            }

            var segments = new List<IntersectionSegment>(edges.Length);
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

                segments.Add(new IntersectionSegment(startIdx, endIdx));
            }

            segments = SplitSegmentsPassingThroughPoints(points, segments);

            var triangulation = Triangulation.Run(in triangle, points, segments);

            var stored = new TrianglePatch[triangulation.Triangles.Count];
            var pairs = coplanarPairs[i];
            for (int p = 0; p < stored.Length; p++)
            {
                var patch = triangulation.Triangles[p];
                int iv0 = GetIntersectionVertexId(in triangle, patch.P0, points, pointVertexIds);
                int iv1 = GetIntersectionVertexId(in triangle, patch.P1, points, pointVertexIds);
                int iv2 = GetIntersectionVertexId(in triangle, patch.P2, points, pointVertexIds);
                var owner = ResolveCoplanarOwner(patch, pairs, otherTriangles);

                stored[p] = new TrianglePatch(
                    patch,
                    triangulation.FaceIds[p],
                    new TriangleVertexIds(iv0, iv1, iv2),
                    owner);
            }

            result[i] = stored;
        }

        return result;
    }

    private static (IReadOnlyList<List<CoplanarPair>?> MeshA, IReadOnlyList<List<CoplanarPair>?> MeshB) BuildCoplanarPairs(
        IntersectionGraph graph,
        IReadOnlyList<Triangle> trianglesA,
        IReadOnlyList<Triangle> trianglesB)
    {
        var coplanarA = new List<CoplanarPair>?[trianglesA.Count];
        var coplanarB = new List<CoplanarPair>?[trianglesB.Count];

        var pairs = graph.Pairs;
        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            var intersection = pair.Intersection;

            var triA = trianglesA[intersection.TriangleIndexA];
            var triB = trianglesB[intersection.TriangleIndexB];

            if (!TrianglePredicates.IsCoplanar(in triA, in triB))
            {
                continue;
            }

            double dot =
                triA.Normal.X * triB.Normal.X +
                triA.Normal.Y * triB.Normal.Y +
                triA.Normal.Z * triB.Normal.Z;

            var owner = dot >= 0.0 ? CoplanarOwner.MeshA : CoplanarOwner.MeshB;

            coplanarA[intersection.TriangleIndexA] ??= new List<CoplanarPair>();
            coplanarA[intersection.TriangleIndexA]!.Add(
                new CoplanarPair(intersection.TriangleIndexB, owner));

            coplanarB[intersection.TriangleIndexB] ??= new List<CoplanarPair>();
            coplanarB[intersection.TriangleIndexB]!.Add(
                new CoplanarPair(intersection.TriangleIndexA, owner));
        }

        return (coplanarA, coplanarB);
    }

    private static CoplanarOwner ResolveCoplanarOwner(
        in RealTriangle patch,
        List<CoplanarPair>? pairs,
        IReadOnlyList<Triangle> otherTriangles)
    {
        if (pairs is null || pairs.Count == 0)
        {
            return CoplanarOwner.None;
        }

        var centroid = patch.Centroid;
        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            var other = otherTriangles[pair.OtherTriangleIndex];
            var otherReal = new RealTriangle(other);
            if (RealTrianglePredicates.IsInsideStrict(otherReal, centroid))
            {
                return pair.Owner;
            }
        }

        return CoplanarOwner.None;
    }

    private readonly struct CoplanarPair
    {
        public int OtherTriangleIndex { get; }
        public CoplanarOwner Owner { get; }

        public CoplanarPair(int otherTriangleIndex, CoplanarOwner owner)
        {
            OtherTriangleIndex = otherTriangleIndex;
            Owner = owner;
        }
    }

    private static (int, int) Normalize(int a, int b) => a < b ? (a, b) : (b, a);

    private static List<IntersectionSegment> SplitSegmentsPassingThroughPoints(
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<IntersectionSegment> segments)
    {
        if (segments.Count == 0 || points.Count < 3)
        {
            return segments as List<IntersectionSegment>
                ?? new List<IntersectionSegment>(segments);
        }

        double interiorTEpsilon = Tolerances.FeatureBarycentricEpsilon;
        double mergeDistanceEpsilon = 10.0 * Tolerances.MergeEpsilon;
        double mergeDistanceEpsilonSquared = mergeDistanceEpsilon * mergeDistanceEpsilon;

        var output = new List<IntersectionSegment>(segments.Count);
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
        IReadOnlyList<IntersectionPoint> points,
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
        List<IntersectionSegment> segments,
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

        segments.Add(new IntersectionSegment(a, b));
    }

    private static int GetIntersectionVertexId(
        in Triangle triangle,
        in RealPoint point,
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<int> pointVertexIds)
    {
        var c0 = new RealPoint(triangle.P0);
        var c1 = new RealPoint(triangle.P1);
        var c2 = new RealPoint(triangle.P2);
        double epsSq = Tolerances.MergeEpsilonSquared;

        if (point.DistanceSquared(in c0) <= epsSq ||
            point.DistanceSquared(in c1) <= epsSq ||
            point.DistanceSquared(in c2) <= epsSq)
        {
            return -1;
        }

        for (int i = 0; i < points.Count; i++)
        {
            var pos = points[i].Position;
            if (point.DistanceSquared(in pos) <= epsSq)
            {
                return pointVertexIds[i];
            }
        }

        return -1;
    }
}
