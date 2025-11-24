using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Assembles a watertight mesh from selected boolean patches by merging
// vertices with quantization and emitting indexed triangles.
public static class BooleanMeshAssembler
{
    public static BooleanMesh Assemble(BooleanPatchSet patchSet)
    {
        if (patchSet is null) throw new ArgumentNullException(nameof(patchSet));

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();

        void AddPatch(in RealTriangle tri)
        {
            int i0 = AddOrGet(vertices, vertexMap, tri.P0);
            int i1 = AddOrGet(vertices, vertexMap, tri.P1);
            int i2 = AddOrGet(vertices, vertexMap, tri.P2);
            triangles.Add((i0, i1, i2));
        }

        foreach (var tri in patchSet.FromMeshA)
        {
            AddPatch(tri);
        }

        foreach (var tri in patchSet.FromMeshB)
        {
            AddPatch(tri);
        }

        ValidateManifoldEdges(triangles);

        return new BooleanMesh(vertices, triangles);
    }

    private static int AddOrGet(
        List<RealPoint> vertices,
        Dictionary<(long X, long Y, long Z), int> map,
        in RealPoint point)
    {
        double eps = Tolerances.TrianglePredicateEpsilon;
        double inv = 1.0 / eps;
        long qx = (long)Math.Round(point.X * inv);
        long qy = (long)Math.Round(point.Y * inv);
        long qz = (long)Math.Round(point.Z * inv);

        var key = (qx, qy, qz);
        if (map.TryGetValue(key, out var existing))
        {
            return existing;
        }

        vertices.Add(point);
        int idx = vertices.Count - 1;
        map[key] = idx;
        return idx;
    }

    private static void ValidateManifoldEdges(IReadOnlyList<(int A, int B, int C)> triangles)
    {
        var edgeUse = new Dictionary<(int, int), int>();

        void AddEdge(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            edgeUse[key] = edgeUse.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 2)
            {
                throw new InvalidOperationException(
                    $"Non-manifold edge detected in boolean mesh assembly: edge {kvp.Key} used {kvp.Value} times (expected 2).");
            }
        }
    }
}
