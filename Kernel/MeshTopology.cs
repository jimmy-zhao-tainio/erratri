using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Shared mesh-local intersection topology between a mesh and the global
// intersection graph. MeshATopology and MeshBTopology are thin wrappers
// over this base; they differ only in which triangle set (A or B) they
// are built from.
public abstract class MeshTopology
{
    // For each triangle in the corresponding IntersectionSet.Triangles*,
    // the set of global intersection edges that lie on it (both endpoints
    // lie on that triangle).
    public IReadOnlyList<IntersectionEdgeId[]> TriangleEdges { get; }

    // All global edges that touch at least one triangle in this mesh.
    public IReadOnlyList<IntersectionEdgeId> Edges { get; }

    // Vertex-edge adjacency restricted to this mesh: for each global
    // intersection vertex, the list of incident edges that lie on
    // triangles of this mesh.
    public IReadOnlyDictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> VertexEdges { get; }

    // Per-component vertex chains on this mesh, expressed as sequences of
    // global IntersectionVertexId. Some components form closed cycles,
    // others may be open chains when local degeneracies are present.
    public IReadOnlyList<IntersectionVertexId[]> Loops { get; }

    protected MeshTopology(
        IntersectionEdgeId[][] triangleEdges,
        IntersectionEdgeId[] edges,
        Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> vertexEdges,
        IntersectionVertexId[][] loops)
    {
        TriangleEdges = triangleEdges ?? throw new ArgumentNullException(nameof(triangleEdges));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        VertexEdges = vertexEdges ?? throw new ArgumentNullException(nameof(vertexEdges));
        Loops = loops ?? throw new ArgumentNullException(nameof(loops));
    }

    // Core builder shared between MeshATopology and MeshBTopology. The caller
    // supplies the per-triangle intersection vertices coming from the
    // TriangleIntersectionIndex for its side (A or B).
    protected static (
        IntersectionEdgeId[][] TriangleEdges,
        IntersectionEdgeId[] Edges,
        Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> VertexEdges,
        IntersectionVertexId[][] Loops)
        BuildCore(
            IntersectionGraph graph,
            IReadOnlyList<TriangleIntersectionVertex[]> perTriangleVertices,
            bool meshA)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (perTriangleVertices is null) throw new ArgumentNullException(nameof(perTriangleVertices));

        int triangleCount = perTriangleVertices.Count;

        // Map each edge id to its endpoints.
        var edgeById = new Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)>();
        foreach (var (id, start, end) in graph.Edges)
        {
            edgeById[id.Value] = (start, end);
        }

        // For each triangle, collect the global intersection edges that truly lie on it.
        //
        // IMPORTANT:
        // It is NOT sufficient to attach an edge to a triangle solely because both of the
        // edge's endpoints are vertices on the triangle. That can incorrectly associate
        // edges that belong to a different triangle (or even a different pair) and feed
        // invalid constraint segments into subdivision, producing missing patches/cracks.
        var triangleEdgeLists = new List<IntersectionEdgeId>[triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            triangleEdgeLists[i] = new List<IntersectionEdgeId>();
        }

        // Edge lookup by undirected endpoint key.
        var edgeIdByEndpoints = new Dictionary<(int Min, int Max), IntersectionEdgeId>(graph.Edges.Count);
        foreach (var (id, start, end) in graph.Edges)
        {
            int a = start.Value;
            int b = end.Value;
            if (b < a) (a, b) = (b, a);
            edgeIdByEndpoints[(a, b)] = id;
        }

        // Quantized world-space -> global vertex id lookup (mirrors IntersectionGraph quantization).
        var globalVertexLookup = new Dictionary<(long X, long Y, long Z), IntersectionVertexId>(graph.Vertices.Count);
        double invEpsilon = 1.0 / Tolerances.TrianglePredicateEpsilon;
        foreach (var (id, position) in graph.Vertices)
        {
            globalVertexLookup[Quantize(position, invEpsilon)] = id;
        }

        var trianglesA = graph.IntersectionSet.TrianglesA ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesA));
        var trianglesB = graph.IntersectionSet.TrianglesB ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesB));

        var pairs = graph.Pairs;

        var localToGlobalVertex = new Dictionary<(int PairIndex, int LocalVertexId), IntersectionVertexId>();
        for (int pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
        {
            var pair = pairs[pairIndex];
            var intersection = pair.Intersection;
            var triangleA = trianglesA[intersection.TriangleIndexA];
            var triangleB = trianglesB[intersection.TriangleIndexB];

            var localVertices = pair.Vertices;
            for (int i = 0; i < localVertices.Count; i++)
            {
                var v = localVertices[i];

                // Prefer A-side reconstruction; fall back to B-side if needed (mirrors TriangleIntersectionIndex).
                var baryA = v.OnTriangleA;
                var worldA = Barycentric.ToRealPointOnTriangle(in triangleA, in baryA);
                if (!globalVertexLookup.TryGetValue(Quantize(worldA, invEpsilon), out var globalId))
                {
                    var baryB = v.OnTriangleB;
                    var worldB = Barycentric.ToRealPointOnTriangle(in triangleB, in baryB);
                    if (!globalVertexLookup.TryGetValue(Quantize(worldB, invEpsilon), out globalId))
                    {
                        System.Diagnostics.Debug.Fail("Global intersection vertex not found for PairVertex.");
                        continue;
                    }
                }

                localToGlobalVertex[(pairIndex, v.VertexId.Value)] = globalId;
            }
        }

        for (int pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
        {
            var pair = pairs[pairIndex];
            var intersection = pair.Intersection;
            int triIndex = meshA ? intersection.TriangleIndexA : intersection.TriangleIndexB;

            // Some meshes (or intermediate states) can have a topology that doesn't match
            // this side's triangle count; guard defensively.
            if ((uint)triIndex >= (uint)triangleCount)
            {
                continue;
            }

            var segments = pair.Segments;
            var dst = triangleEdgeLists[triIndex];

            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];

                if (!localToGlobalVertex.TryGetValue((pairIndex, s.Start.VertexId.Value), out var startId) ||
                    !localToGlobalVertex.TryGetValue((pairIndex, s.End.VertexId.Value), out var endId))
                {
                    continue;
                }

                int a = startId.Value;
                int b = endId.Value;
                if (b < a) (a, b) = (b, a);

                if (!edgeIdByEndpoints.TryGetValue((a, b), out var edgeId))
                {
                    System.Diagnostics.Debug.Fail("Edge not found for pair segment endpoints.");
                    continue;
                }

                dst.Add(edgeId);
            }
        }

        for (int i = 0; i < triangleEdgeLists.Length; i++)
        {
            var list = triangleEdgeLists[i];
            if (list.Count <= 1)
            {
                continue;
            }

            var seen = new HashSet<int>();
            int write = 0;
            for (int j = 0; j < list.Count; j++)
            {
                var e = list[j];
                if (seen.Add(e.Value))
                {
                    list[write++] = e;
                }
            }

            if (write != list.Count)
            {
                list.RemoveRange(write, list.Count - write);
            }
        }

        // Build vertex-edge adjacency restricted to edges that lie on this mesh.
        var vertexAdjacency = new Dictionary<IntersectionVertexId, List<IntersectionEdgeId>>();
        var meshEdgesSet = new HashSet<int>();

        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            var edgesOnTriangle = triangleEdgeLists[triIndex];
            for (int i = 0; i < edgesOnTriangle.Count; i++)
            {
                var edgeId = edgesOnTriangle[i];
                if (!meshEdgesSet.Add(edgeId.Value))
                {
                    continue; // Already accounted for this edge in adjacency.
                }

                var endpoints = edgeById[edgeId.Value];
                AddEdgeToAdjacency(vertexAdjacency, endpoints.Start, edgeId);
                AddEdgeToAdjacency(vertexAdjacency, endpoints.End, edgeId);
            }
        }

        // Convert adjacency lists to read-only views.
        var vertexEdges = new Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>>(vertexAdjacency.Count);
        foreach (var kvp in vertexAdjacency)
        {
            vertexEdges.Add(kvp.Key, kvp.Value.AsReadOnly());
        }

        // Convert per-triangle edges to arrays.
        var triangleEdges = new IntersectionEdgeId[triangleCount][];
        for (int i = 0; i < triangleCount; i++)
        {
            var list = triangleEdgeLists[i];
            triangleEdges[i] = list.Count == 0 ? Array.Empty<IntersectionEdgeId>() : list.ToArray();
        }

        // Flatten mesh edge ids into a list.
        var meshEdges = new List<IntersectionEdgeId>(meshEdgesSet.Count);
        foreach (var edgeValue in meshEdgesSet)
        {
            meshEdges.Add(new IntersectionEdgeId(edgeValue));
        }

        // Extract closed loops over this mesh.
        var loops = ExtractLoops(vertexAdjacency, edgeById, meshEdgesSet);

        return (
            triangleEdges,
            meshEdges.ToArray(),
            vertexEdges,
            loops.ToArray());
    }

    private static (long X, long Y, long Z) Quantize(RealPoint point, double invEpsilon)
    {
        long qx = (long)Math.Round(point.X * invEpsilon);
        long qy = (long)Math.Round(point.Y * invEpsilon);
        long qz = (long)Math.Round(point.Z * invEpsilon);
        return (qx, qy, qz);
    }

    private static void AddEdgeToAdjacency(
        Dictionary<IntersectionVertexId, List<IntersectionEdgeId>> adjacency,
        IntersectionVertexId vertex,
        IntersectionEdgeId edge)
    {
        if (!adjacency.TryGetValue(vertex, out var list))
        {
            list = new List<IntersectionEdgeId>();
            adjacency[vertex] = list;
        }

        list.Add(edge);
    }

    private static List<IntersectionVertexId[]> ExtractLoops(
        Dictionary<IntersectionVertexId, List<IntersectionEdgeId>> adjacency,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById,
        HashSet<int> meshEdges)
    {
        var remainingEdges = new HashSet<int>(meshEdges);
        var loops = new List<IntersectionVertexId[]>();

        while (remainingEdges.Count > 0)
        {
            // Pick an arbitrary remaining edge to seed the next loop.
            int seedEdgeValue = 0;
            foreach (var value in remainingEdges)
            {
                seedEdgeValue = value;
                break;
            }

            var endpoints = edgeById[seedEdgeValue];
            var startVertex = endpoints.Start;
            var currentVertex = endpoints.End;

            var loop = new List<IntersectionVertexId>
            {
                startVertex,
                currentVertex
            };

            remainingEdges.Remove(seedEdgeValue);

            while (true)
            {
                if (!adjacency.TryGetValue(currentVertex, out var incidentEdges))
                {
                    break;
                }

                IntersectionEdgeId nextEdge = default;
                bool foundNext = false;

                for (int i = 0; i < incidentEdges.Count; i++)
                {
                    var candidate = incidentEdges[i];
                    if (remainingEdges.Contains(candidate.Value))
                    {
                        nextEdge = candidate;
                        foundNext = true;
                        break;
                    }
                }

                if (!foundNext)
                {
                    // No unused incident edge from this vertex; loop should be closed.
                    break;
                }

                remainingEdges.Remove(nextEdge.Value);
                var nextEndpoints = edgeById[nextEdge.Value];
                var nextVertex = nextEndpoints.Start.Value == currentVertex.Value
                    ? nextEndpoints.End
                    : nextEndpoints.Start;

                if (nextVertex.Value == startVertex.Value)
                {
                    // Close the cycle.
                    loop.Add(startVertex);
                    break;
                }

                loop.Add(nextVertex);
                currentVertex = nextVertex;
            }

            loops.Add(loop.ToArray());
        }

        return loops;
    }
}
