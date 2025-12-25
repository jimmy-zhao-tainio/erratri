using System;
using System.Collections.Generic;
using Geometry;
using Boolean;

namespace Boolean.Intersection.Graph.Index;

// One intersection vertex attached to a specific triangle, expressed in
// barycentric coordinates on that triangle plus the shared global vertex id.
public readonly struct TriangleIntersectionVertex
{
    public IntersectionVertexId VertexId { get; }

    public Barycentric Barycentric { get; }

    public TriangleIntersectionVertex(IntersectionVertexId vertexId, Barycentric barycentric)
    {
        VertexId = vertexId;
        Barycentric = barycentric;
    }
}

// Per-triangle index of all intersection vertices on meshes A and B.
//
// For each triangle in IntersectionSet.TrianglesA and TrianglesB we store
// the list of global intersection vertices that lie on it together with
// their barycentric coordinates on that triangle.
public sealed class TriangleIntersectionIndex
{
    // TrianglesA[i] lists all intersection vertices lying on
    // IntersectionSet.TrianglesA[i].
    public IReadOnlyList<TriangleIntersectionVertex[]> TrianglesA { get; }

    // TrianglesB[j] lists all intersection vertices lying on
    // IntersectionSet.TrianglesB[j].
    public IReadOnlyList<TriangleIntersectionVertex[]> TrianglesB { get; }

    private TriangleIntersectionIndex(
        TriangleIntersectionVertex[][] trianglesA,
        TriangleIntersectionVertex[][] trianglesB)
    {
        TrianglesA = trianglesA ?? throw new ArgumentNullException(nameof(trianglesA));
        TrianglesB = trianglesB ?? throw new ArgumentNullException(nameof(trianglesB));
    }

    public static TriangleIntersectionIndex Run(IntersectionGraph graph)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));

        var set = graph.IntersectionSet;
        var trianglesA = set.TrianglesA ?? throw new ArgumentNullException(nameof(set.TrianglesA));
        var trianglesB = set.TrianglesB ?? throw new ArgumentNullException(nameof(set.TrianglesB));

        int countA = trianglesA.Count;
        int countB = trianglesB.Count;

        var perTriangleA = new List<TriangleIntersectionVertex>[countA];
        var perTriangleB = new List<TriangleIntersectionVertex>[countB];

        for (int i = 0; i < countA; i++)
        {
            perTriangleA[i] = new List<TriangleIntersectionVertex>();
        }

        for (int i = 0; i < countB; i++)
        {
            perTriangleB[i] = new List<TriangleIntersectionVertex>();
        }

        // Build a lookup from quantized world-space position to global
        // IntersectionVertexId using the same quantization scheme as
        // IntersectionGraph.FromIntersectionSet.
        var globalVertexLookup = new Dictionary<(long X, long Y, long Z), IntersectionVertexId>();
        double invEpsilon = 1.0 / Tolerances.TrianglePredicateEpsilon;

        foreach (var (id, position) in graph.Vertices)
        {
            var key = Quantize(position, invEpsilon);
            if (!globalVertexLookup.ContainsKey(key))
            {
                globalVertexLookup.Add(key, id);
            }
        }

        var pairs = graph.Pairs;

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

                // Use barycentric coordinates on triangle A to reconstruct
                // the shared world-space point, then map back to the global
                // IntersectionVertexId via the quantized lookup. If the A-side
                // quantization misses (numerical asymmetry when one endpoint
                // lies on B's edge/interior), fall back to B-side quantization
                // so both triangles attach the shared vertex/edge.
                var barycentricOnA = v.OnTriangleA;
                var worldA = Barycentric.ToRealPointOnTriangle(in triangleA, in barycentricOnA);
                var keyA = Quantize(worldA, invEpsilon);

                IntersectionVertexId globalId;
                if (!globalVertexLookup.TryGetValue(keyA, out globalId))
                {
                    var barycentricOnB = v.OnTriangleB;
                    var worldB = Barycentric.ToRealPointOnTriangle(in triangleB, in barycentricOnB);
                    var keyB = Quantize(worldB, invEpsilon);

                    if (!globalVertexLookup.TryGetValue(keyB, out globalId))
                    {
                        System.Diagnostics.Debug.Fail("Global intersection vertex not found for PairVertex.");
                        continue;
                    }
                }

                // Attach to triangle A if the barycentric is inside (inclusive).
                if (v.OnTriangleA.IsInsideInclusive())
                {
                    var listA = perTriangleA[intersection.TriangleIndexA];
                    if (!ContainsVertex(listA, globalId))
                    {
                        listA.Add(new TriangleIntersectionVertex(globalId, v.OnTriangleA));
                    }
                }

                // Attach to triangle B if the barycentric is inside (inclusive).
                if (v.OnTriangleB.IsInsideInclusive())
                {
                    var listB = perTriangleB[intersection.TriangleIndexB];
                    if (!ContainsVertex(listB, globalId))
                    {
                        listB.Add(new TriangleIntersectionVertex(globalId, v.OnTriangleB));
                    }
                }
            }
        }

        // Propagate edge vertices across shared mesh edges so that if an
        // intersection vertex lies on a mesh edge, it is attached to both
        // incident triangles even when only one triangle participates in pairs.
        PropagateEdgeVertices(trianglesA, perTriangleA, graph.Vertices);
        PropagateEdgeVertices(trianglesB, perTriangleB, graph.Vertices);

        var resultA = new TriangleIntersectionVertex[countA][];
        for (int i = 0; i < countA; i++)
        {
            var list = perTriangleA[i];
            resultA[i] = list.Count == 0 ? Array.Empty<TriangleIntersectionVertex>() : list.ToArray();
        }

        var resultB = new TriangleIntersectionVertex[countB][];
        for (int i = 0; i < countB; i++)
        {
            var list = perTriangleB[i];
            resultB[i] = list.Count == 0 ? Array.Empty<TriangleIntersectionVertex>() : list.ToArray();
        }

        return new TriangleIntersectionIndex(resultA, resultB);
    }

    private static void PropagateEdgeVertices(
        IReadOnlyList<Triangle> triangles,
        List<TriangleIntersectionVertex>[] perTriangle,
        IReadOnlyList<(IntersectionVertexId Id, RealPoint Position)> globalVertices)
    {
        if (triangles.Count != perTriangle.Length)
        {
            return;
        }

        var adjacency = BuildEdgeAdjacency(triangles);

        var inQueue = new bool[triangles.Count];
        var queue = new Queue<int>();
        for (int i = 0; i < perTriangle.Length; i++)
        {
            if (perTriangle[i].Count == 0)
            {
                continue;
            }

            inQueue[i] = true;
            queue.Enqueue(i);
        }

        while (queue.Count > 0)
        {
            int triIndex = queue.Dequeue();
            inQueue[triIndex] = false;

            var triangle = triangles[triIndex];
            var list = perTriangle[triIndex];

            for (int i = 0; i < list.Count; i++)
            {
                var vertex = list[i];
                int edgeMask = GetEdgeMask(vertex.Barycentric, Tolerances.BarycentricInsideEpsilon);
                if (edgeMask == 0)
                {
                    continue;
                }

                var world = globalVertices[vertex.VertexId.Value].Position;

                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    if ((edgeMask & (1 << edgeIndex)) == 0)
                    {
                        continue;
                    }

                    GetEdgeEndpoints(in triangle, edgeIndex, out var a, out var b);
                    var key = NormalizeEdgeKey(in a, in b);
                    if (!adjacency.TryGetValue(key, out var entry) || entry.Count != 2)
                    {
                        continue;
                    }

                    int other = entry.Tri0 == triIndex ? entry.Tri1 : entry.Tri0;
                    if ((uint)other >= (uint)triangles.Count)
                    {
                        continue;
                    }

                    var otherList = perTriangle[other];
                    if (ContainsVertex(otherList, vertex.VertexId))
                    {
                        continue;
                    }

                    var barycentricOnOther = ComputeBarycentricOnTriangle(triangles[other], in world);
                    if (!barycentricOnOther.IsInsideInclusive())
                    {
                        continue;
                    }

                    otherList.Add(new TriangleIntersectionVertex(vertex.VertexId, barycentricOnOther));
                    if (!inQueue[other])
                    {
                        inQueue[other] = true;
                        queue.Enqueue(other);
                    }
                }
            }
        }
    }

    private static Barycentric ComputeBarycentricOnTriangle(Triangle triangle, in RealPoint point)
    {
        var realTriangle = new RealTriangle(triangle);
        var barycentric = realTriangle.ComputeBarycentric(in point, out double denom);
        if (denom == 0.0)
        {
            return new Barycentric(0.0, 0.0, 0.0);
        }

        return barycentric;
    }

    private static int GetEdgeMask(in Barycentric b, double eps)
    {
        int mask = 0;
        if (Math.Abs(b.U) <= eps) mask |= 1 << 0; // edge P1-P2
        if (Math.Abs(b.V) <= eps) mask |= 1 << 1; // edge P2-P0
        if (Math.Abs(b.W) <= eps) mask |= 1 << 2; // edge P0-P1
        return mask;
    }

    private static void GetEdgeEndpoints(in Triangle triangle, int edgeIndex, out Point a, out Point b)
    {
        switch (edgeIndex)
        {
            case 0:
                a = triangle.P1;
                b = triangle.P2;
                return;
            case 1:
                a = triangle.P2;
                b = triangle.P0;
                return;
            default:
                a = triangle.P0;
                b = triangle.P1;
                return;
        }
    }

    private readonly struct EdgeAdjacencyEntry
    {
        public readonly int Tri0;
        public readonly int Tri1;
        public readonly int Count;

        public EdgeAdjacencyEntry(int tri0, int tri1, int count)
        {
            Tri0 = tri0;
            Tri1 = tri1;
            Count = count;
        }
    }

    private static Dictionary<(Point A, Point B), EdgeAdjacencyEntry> BuildEdgeAdjacency(IReadOnlyList<Triangle> triangles)
    {
        var adjacency = new Dictionary<(Point A, Point B), EdgeAdjacencyEntry>();

        for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
        {
            var tri = triangles[triIndex];

            AddEdge(in tri.P0, in tri.P1, triIndex);
            AddEdge(in tri.P1, in tri.P2, triIndex);
            AddEdge(in tri.P2, in tri.P0, triIndex);
        }

        return adjacency;

        void AddEdge(in Point p0, in Point p1, int triIndex)
        {
            var key = NormalizeEdgeKey(in p0, in p1);
            if (!adjacency.TryGetValue(key, out var entry))
            {
                adjacency.Add(key, new EdgeAdjacencyEntry(triIndex, tri1: -1, count: 1));
                return;
            }

            if (entry.Count == 1)
            {
                adjacency[key] = new EdgeAdjacencyEntry(entry.Tri0, triIndex, count: 2);
                return;
            }

            adjacency[key] = new EdgeAdjacencyEntry(tri0: -1, tri1: -1, count: 3);
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

    private static (long X, long Y, long Z) Quantize(RealPoint point, double invEpsilon)
    {
        long qx = (long)Math.Round(point.X * invEpsilon);
        long qy = (long)Math.Round(point.Y * invEpsilon);
        long qz = (long)Math.Round(point.Z * invEpsilon);
        return (qx, qy, qz);
    }

    private static bool ContainsVertex(List<TriangleIntersectionVertex> list, IntersectionVertexId vertexId)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].VertexId.Value == vertexId.Value)
            {
                return true;
            }
        }

        return false;
    }
}
