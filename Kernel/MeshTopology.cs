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

        var vertexPositions = new RealPoint[graph.Vertices.Count];
        foreach (var (id, position) in graph.Vertices)
        {
            if ((uint)id.Value < (uint)vertexPositions.Length)
            {
                vertexPositions[id.Value] = position;
            }
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

                if (edgeIdByEndpoints.TryGetValue((a, b), out var edgeId))
                {
                    dst.Add(edgeId);
                    continue;
                }

                // If the global graph has been split by intermediate vertices, a pair-local
                // segment may represent a "super-edge". Expand it to the existing chain.
                if (!TryExpandSegmentToEdgeChain(
                        startId.Value,
                        endId.Value,
                        vertexPositions,
                        edgeIdByEndpoints,
                        dst))
                {
                    System.Diagnostics.Debug.Fail("Edge not found for pair segment endpoints.");
                }
            }
        }

        // Edge propagation across shared mesh edges:
        // If an intersection edge lies on an original mesh edge, ensure it is attached to both
        // triangles incident to that mesh edge. This prevents cracks when one triangle does not
        // appear in graph.Pairs (and would otherwise have no constraints) but shares a mesh edge
        // containing intersection vertices with a neighboring triangle that does.
        PropagateEdgesAcrossSharedMeshEdges(
            meshA ? trianglesA : trianglesB,
            perTriangleVertices,
            triangleEdgeLists,
            edgeById);

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

    private static void PropagateEdgesAcrossSharedMeshEdges(
        IReadOnlyList<Triangle> triangles,
        IReadOnlyList<TriangleIntersectionVertex[]> perTriangleVertices,
        IReadOnlyList<List<IntersectionEdgeId>> triangleEdgeLists,
        IReadOnlyDictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById)
    {
        if (triangles.Count != perTriangleVertices.Count ||
            triangles.Count != triangleEdgeLists.Count)
        {
            return;
        }

        var sharedEdges = BuildTriangleAdjacency(triangles);
        if (sharedEdges.Count == 0)
        {
            return;
        }

        var scratchEdges0 = new List<IntersectionEdgeId>();
        var scratchEdges1 = new List<IntersectionEdgeId>();
        var union = new HashSet<int>();

        foreach (var kvp in sharedEdges)
        {
            var adj = kvp.Value;
            if (adj.Count != 2)
            {
                continue;
            }

            int t0 = adj.Tri0;
            int e0 = adj.Edge0;
            int t1 = adj.Tri1;
            int e1 = adj.Edge1;

            scratchEdges0.Clear();
            scratchEdges1.Clear();
            CollectEdgesOnTriangleEdge(perTriangleVertices[t0], triangleEdgeLists[t0], edgeById, e0, scratchEdges0);
            CollectEdgesOnTriangleEdge(perTriangleVertices[t1], triangleEdgeLists[t1], edgeById, e1, scratchEdges1);

            if (scratchEdges0.Count == 0 && scratchEdges1.Count == 0)
            {
                continue;
            }

            union.Clear();
            AddEdgeValues(union, scratchEdges0);
            AddEdgeValues(union, scratchEdges1);

            foreach (int edgeValue in union)
            {
                if (!edgeById.TryGetValue(edgeValue, out var endpoints))
                {
                    continue;
                }

                // Avoid attaching edges whose endpoints are not attached as vertices on the triangle
                // (TrianglePatchSet would otherwise fail when converting edge endpoints to point indices).
                if (ContainsVertex(perTriangleVertices[t0], endpoints.Start) &&
                    ContainsVertex(perTriangleVertices[t0], endpoints.End) &&
                    !ContainsEdge(triangleEdgeLists[t0], edgeValue))
                {
                    triangleEdgeLists[t0].Add(new IntersectionEdgeId(edgeValue));
                }

                if (ContainsVertex(perTriangleVertices[t1], endpoints.Start) &&
                    ContainsVertex(perTriangleVertices[t1], endpoints.End) &&
                    !ContainsEdge(triangleEdgeLists[t1], edgeValue))
                {
                    triangleEdgeLists[t1].Add(new IntersectionEdgeId(edgeValue));
                }
            }
        }
    }

    private static void CollectEdgesOnTriangleEdge(
        TriangleIntersectionVertex[] verticesOnTriangle,
        List<IntersectionEdgeId> triangleEdges,
        IReadOnlyDictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById,
        int edgeIndex,
        List<IntersectionEdgeId> output)
    {
        output.Clear();
        if (triangleEdges.Count == 0 || verticesOnTriangle.Length == 0)
        {
            return;
        }

        var verticesOnEdge = new HashSet<int>();
        for (int i = 0; i < verticesOnTriangle.Length; i++)
        {
            var v = verticesOnTriangle[i];
            if (GetEdgeMask(v.Barycentric, Tolerances.BarycentricInsideEpsilon).HasFlag(edgeIndex))
            {
                verticesOnEdge.Add(v.VertexId.Value);
            }
        }

        if (verticesOnEdge.Count == 0)
        {
            return;
        }

        for (int i = 0; i < triangleEdges.Count; i++)
        {
            int edgeValue = triangleEdges[i].Value;
            if (!edgeById.TryGetValue(edgeValue, out var endpoints))
            {
                continue;
            }

            if (verticesOnEdge.Contains(endpoints.Start.Value) && verticesOnEdge.Contains(endpoints.End.Value))
            {
                output.Add(new IntersectionEdgeId(edgeValue));
            }
        }
    }

    private static void AddEdgeValues(HashSet<int> dst, List<IntersectionEdgeId> edges)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            dst.Add(edges[i].Value);
        }
    }

    private readonly struct EdgeMask
    {
        private readonly int _mask;
        public EdgeMask(int mask) => _mask = mask;
        public bool HasFlag(int edgeIndex) => (_mask & (1 << edgeIndex)) != 0;
    }

    private static EdgeMask GetEdgeMask(in Barycentric b, double eps)
    {
        int mask = 0;
        if (Math.Abs(b.U) <= eps) mask |= 1 << 0; // edge P1-P2
        if (Math.Abs(b.V) <= eps) mask |= 1 << 1; // edge P2-P0
        if (Math.Abs(b.W) <= eps) mask |= 1 << 2; // edge P0-P1
        return new EdgeMask(mask);
    }

    private static bool ContainsVertex(TriangleIntersectionVertex[] vertices, IntersectionVertexId vertexId)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].VertexId.Value == vertexId.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsEdge(List<IntersectionEdgeId> edges, int edgeValue)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].Value == edgeValue)
            {
                return true;
            }
        }

        return false;
    }

    private readonly struct TriangleAdjacencyEntry
    {
        public readonly int Tri0;
        public readonly int Edge0;
        public readonly int Tri1;
        public readonly int Edge1;
        public readonly int Count;

        public TriangleAdjacencyEntry(int tri0, int edge0, int tri1, int edge1, int count)
        {
            Tri0 = tri0;
            Edge0 = edge0;
            Tri1 = tri1;
            Edge1 = edge1;
            Count = count;
        }
    }

    private static Dictionary<(Point A, Point B), TriangleAdjacencyEntry> BuildTriangleAdjacency(IReadOnlyList<Triangle> triangles)
    {
        var adjacency = new Dictionary<(Point A, Point B), TriangleAdjacencyEntry>();

        for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
        {
            var tri = triangles[triIndex];
            AddEdge(in tri.P1, in tri.P2, triIndex, edgeIndex: 0);
            AddEdge(in tri.P2, in tri.P0, triIndex, edgeIndex: 1);
            AddEdge(in tri.P0, in tri.P1, triIndex, edgeIndex: 2);
        }

        return adjacency;

        void AddEdge(in Point p0, in Point p1, int triIndex, int edgeIndex)
        {
            var key = NormalizeEdgeKey(in p0, in p1);
            if (!adjacency.TryGetValue(key, out var entry))
            {
                adjacency.Add(key, new TriangleAdjacencyEntry(triIndex, edgeIndex, tri1: -1, edge1: -1, count: 1));
                return;
            }

            if (entry.Count == 1)
            {
                adjacency[key] = new TriangleAdjacencyEntry(entry.Tri0, entry.Edge0, triIndex, edgeIndex, count: 2);
                return;
            }

            adjacency[key] = new TriangleAdjacencyEntry(tri0: -1, edge0: -1, tri1: -1, edge1: -1, count: 3);
        }
    }

    private static (Point A, Point B) NormalizeEdgeKey(in Point a, in Point b)
    {
        return ComparePoints(in a, in b) <= 0 ? (a, b) : (b, a);
    }

    private static int ComparePoints(in Point a, in Point b)
    {
        int cmp = a.X.CompareTo(b.X);
        if (cmp != 0) return cmp;
        cmp = a.Y.CompareTo(b.Y);
        if (cmp != 0) return cmp;
        return a.Z.CompareTo(b.Z);
    }

    private static bool TryExpandSegmentToEdgeChain(
        int startVertexValue,
        int endVertexValue,
        RealPoint[] vertexPositions,
        Dictionary<(int Min, int Max), IntersectionEdgeId> edgeIdByEndpoints,
        List<IntersectionEdgeId> output)
    {
        if ((uint)startVertexValue >= (uint)vertexPositions.Length ||
            (uint)endVertexValue >= (uint)vertexPositions.Length ||
            startVertexValue == endVertexValue)
        {
            return false;
        }

        var start = vertexPositions[startVertexValue];
        var end = vertexPositions[endVertexValue];

        var ab = RealVector.FromPoints(in start, in end);
        double abLenSq = ab.Dot(in ab);
        if (abLenSq <= 0.0)
        {
            return false;
        }

        double tEps = Tolerances.FeatureBarycentricEpsilon;
        double distanceEpsilon = 10.0 * Tolerances.MergeEpsilon;
        double distanceEpsilonSquared = distanceEpsilon * distanceEpsilon;

        var interior = new List<(double T, int VertexValue)>();

        for (int v = 0; v < vertexPositions.Length; v++)
        {
            if (v == startVertexValue || v == endVertexValue)
            {
                continue;
            }

            var p = vertexPositions[v];
            var ap = RealVector.FromPoints(in start, in p);
            double t = ap.Dot(in ab) / abLenSq;
            if (t <= tEps || t >= 1.0 - tEps)
            {
                continue;
            }

            var closest = Lerp(in start, in end, t);
            if (p.DistanceSquared(in closest) > distanceEpsilonSquared)
            {
                continue;
            }

            interior.Add((t, v));
        }

        if (interior.Count == 0)
        {
            return false;
        }

        interior.Sort(static (a, b) => a.T.CompareTo(b.T));

        int prev = startVertexValue;
        for (int i = 0; i < interior.Count; i++)
        {
            int next = interior[i].VertexValue;
            if (!TryAddEdge(prev, next))
            {
                return false;
            }
            prev = next;
        }

        return TryAddEdge(prev, endVertexValue);

        bool TryAddEdge(int a, int b)
        {
            if (a == b)
            {
                return true;
            }

            if (b < a)
            {
                (a, b) = (b, a);
            }

            if (!edgeIdByEndpoints.TryGetValue((a, b), out var edgeId))
            {
                return false;
            }

            output.Add(edgeId);
            return true;
        }
    }

    private static RealPoint Lerp(in RealPoint a, in RealPoint b, double t)
    {
        return new RealPoint(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
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
