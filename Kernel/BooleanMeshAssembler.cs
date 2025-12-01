using System;
using System.Collections.Generic;
using Geometry;
using Topology;

namespace Kernel;

// Assembles a watertight mesh from selected boolean patches by merging
// vertices with quantization and emitting indexed triangles.
public static class BooleanMeshAssembler
{
    public static RealMesh Assemble(BooleanPatchSet patchSet)
    {
        if (patchSet is null) throw new ArgumentNullException(nameof(patchSet));

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();

        void AddPatch(in RealTriangle triangle)
        {
            if (RealTriangle.HasZeroArea(triangle.P0, triangle.P1, triangle.P2))
            {
                // Drop degenerate triangle
                return;
            }

            int i0 = AddOrGet(vertices, vertexMap, triangle.P0);
            int i1 = AddOrGet(vertices, vertexMap, triangle.P1);
            int i2 = AddOrGet(vertices, vertexMap, triangle.P2);

            // Drop triangles that collapse due to vertex merge
            if (i0 == i1 || i1 == i2 || i2 == i0)
            {
                return;
            }

            triangles.Add((i0, i1, i2));
        }

        foreach (var triangle in patchSet.FromMeshA)
        {
            AddPatch(triangle);
        }

        foreach (var triangle in patchSet.FromMeshB)
        {
            AddPatch(triangle);
        }

        // Enforce manifoldness: every edge must be used exactly twice.
        ValidateManifoldEdges(vertices, triangles);

        return new RealMesh(vertices, triangles);
    }

    private static int AddOrGet(
        List<RealPoint> vertices,
        Dictionary<(long X, long Y, long Z), int> map,
        in RealPoint point)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;
        double inv = 1.0 / epsilon;
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

    private static void ValidateManifoldEdges(
        IReadOnlyList<RealPoint> vertices,
        IReadOnlyList<(int A, int B, int C)> triangles)
    {
        var edgeUse = new Dictionary<(int, int), int>();
        var idToPosition = new Dictionary<int, RealPoint>();
        var voxelToIds = new Dictionary<(int, int, int), List<int>>();

        void AddEdge(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            edgeUse[key] = edgeUse.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (aIdx, bIdx, cIdx) = triangles[i];
            var pa = vertices[aIdx];
            var pb = vertices[bIdx];
            var pc = vertices[cIdx];

            int ca = VertexCanonicalizer.GetOrAddCanonicalId(pa, idToPosition, voxelToIds);
            int cb = VertexCanonicalizer.GetOrAddCanonicalId(pb, idToPosition, voxelToIds);
            int cc = VertexCanonicalizer.GetOrAddCanonicalId(pc, idToPosition, voxelToIds);

            AddEdge(ca, cb);
            AddEdge(cb, cc);
            AddEdge(cc, ca);
        }

        var nonManifold = new List<((int, int) Edge, int Count)>();

        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 2)
            {
                nonManifold.Add((kvp.Key, kvp.Value));
            }
        }

        if (nonManifold.Count > 0)
        {
            var parts = new List<string>(nonManifold.Count);
            foreach (var f in nonManifold)
            {
                parts.Add($"edge {f.Edge} used {f.Count} times");
            }

            string summary = string.Join("; ", parts);
            string message =
                $"Non-manifold edges detected in boolean mesh assembly ({nonManifold.Count}). ";
        //        $"Expected every edge to be used exactly 2 times, but found: {summary}.";

            throw new InvalidOperationException(message);
        }
    }

}

internal static class VertexCanonicalizer
{
    public static int GetOrAddCanonicalId(
        RealPoint p,
        Dictionary<int, RealPoint> idToPosition,
        Dictionary<(int, int, int), List<int>> voxelToIds)
    {
        double inv = 1.0 / Tolerances.MergeEpsilon;
        int vx = (int)Math.Floor(p.X * inv);
        int vy = (int)Math.Floor(p.Y * inv);
        int vz = (int)Math.Floor(p.Z * inv);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    var key = (vx + dx, vy + dy, vz + dz);
                    if (!voxelToIds.TryGetValue(key, out var candidates))
                    {
                        continue;
                    }

                    foreach (var id in candidates)
                    {
                        var c = idToPosition[id];

                        if (p.DistanceSquared(in c) <= Tolerances.MergeEpsilonSquared)
                        {
                            return id;
                        }
                    }
                }
            }
        }

        int newId = idToPosition.Count;
        idToPosition[newId] = p;
        var home = (vx, vy, vz);
        if (!voxelToIds.TryGetValue(home, out var list))
        {
            list = new List<int>();
            voxelToIds[home] = list;
        }
        list.Add(newId);
        return newId;
    }
}
