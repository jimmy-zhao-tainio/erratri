using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

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

    public static TriangleIntersectionIndex Build(IntersectionGraph graph)
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

            var triA = trianglesA[intersection.TriangleIndexA];
            var triB = trianglesB[intersection.TriangleIndexB];

            var localVertices = pair.Vertices;

            for (int i = 0; i < localVertices.Count; i++)
            {
                var v = localVertices[i];

                // Use barycentric coordinates on triangle A to reconstruct
                // the shared world-space point, then map back to the global
                // IntersectionVertexId via the quantized lookup.
                var baryOnA = v.OnTriangleA;
                var world = Barycentric.ToRealPointOnTriangle(in triA, in baryOnA);
                var key = Quantize(world, invEpsilon);

                if (!globalVertexLookup.TryGetValue(key, out var globalId))
                {
                    System.Diagnostics.Debug.Fail("Global intersection vertex not found for PairVertex.");
                    continue;
                }

                // Attach to triangle A.
                var listA = perTriangleA[intersection.TriangleIndexA];
                if (!ContainsVertex(listA, globalId))
                {
                    listA.Add(new TriangleIntersectionVertex(globalId, v.OnTriangleA));
                }

                // Attach to triangle B.
                var listB = perTriangleB[intersection.TriangleIndexB];
                if (!ContainsVertex(listB, globalId))
                {
                    listB.Add(new TriangleIntersectionVertex(globalId, v.OnTriangleB));
                }
            }
        }

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
