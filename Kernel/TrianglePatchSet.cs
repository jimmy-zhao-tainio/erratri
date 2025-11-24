using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Per-mesh triangle patches obtained by cutting each triangle along the
// intersection segments that lie on it. This is the mesh-level wrapper
// around TriangleSubdivision.
public sealed class TrianglePatchSet
{
    // Patches for each triangle in IntersectionSet.TrianglesA.
    // Index i corresponds to triangle i in mesh A.
    public IReadOnlyList<IReadOnlyList<RealTriangle>> TrianglesA { get; }

    // Patches for each triangle in IntersectionSet.TrianglesB.
    // Index j corresponds to triangle j in mesh B.
    public IReadOnlyList<IReadOnlyList<RealTriangle>> TrianglesB { get; }

    private TrianglePatchSet(
        IReadOnlyList<IReadOnlyList<RealTriangle>> trianglesA,
        IReadOnlyList<IReadOnlyList<RealTriangle>> trianglesB)
    {
        TrianglesA = trianglesA ?? throw new ArgumentNullException(nameof(trianglesA));
        TrianglesB = trianglesB ?? throw new ArgumentNullException(nameof(trianglesB));
    }

    public static TrianglePatchSet Build(
        IntersectionGraph graph,
        TriangleIntersectionIndex index,
        MeshATopology topologyA,
        MeshBTopology topologyB)
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

        return new TrianglePatchSet(patchesA, patchesB);
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

            var points = new List<TriangleSubdivision.IntersectionPoint>(vertices.Length);
            var pointIndexByVertexId = new Dictionary<int, int>(vertices.Length);

            for (int v = 0; v < vertices.Length; v++)
            {
                var tiv = vertices[v];
                var bary = tiv.Barycentric;
                var world = triangle.FromBarycentric(in bary);
                pointIndexByVertexId[tiv.VertexId.Value] = points.Count;
                points.Add(new TriangleSubdivision.IntersectionPoint(tiv.Barycentric, world));
            }

            var segments = new List<TriangleSubdivision.IntersectionSegment>(edges.Length);
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

                segments.Add(new TriangleSubdivision.IntersectionSegment(startIdx, endIdx));
            }

            var patches = TriangleSubdivision.Subdivide(in triangle, points, segments);
            var stored = patches is List<RealTriangle> list ? list : new List<RealTriangle>(patches);
            result[i] = stored.ToArray();
        }

        return result;
    }

    private static (int, int) Normalize(int a, int b) => a < b ? (a, b) : (b, a);
}
