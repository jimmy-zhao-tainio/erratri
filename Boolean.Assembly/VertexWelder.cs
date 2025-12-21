using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

internal readonly struct VertexWeldStats
{
    public int VerticesBefore { get; }
    public int VerticesAfter { get; }
    public int TrianglesBefore { get; }
    public int TrianglesAfter { get; }
    public int CollapsedRemoved { get; }

    public VertexWeldStats(int verticesBefore, int verticesAfter, int trianglesBefore, int trianglesAfter, int collapsedRemoved)
    {
        VerticesBefore = verticesBefore;
        VerticesAfter = verticesAfter;
        TrianglesBefore = trianglesBefore;
        TrianglesAfter = trianglesAfter;
        CollapsedRemoved = collapsedRemoved;
    }
}

internal static class VertexWelder
{
    public static VertexWeldStats WeldInPlace(
        List<RealPoint> vertices,
        List<(int A, int B, int C)> triangles,
        double epsilon)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        if (vertices.Count == 0 || triangles.Count == 0)
        {
            return new VertexWeldStats(vertices.Count, vertices.Count, triangles.Count, triangles.Count, 0);
        }

        int verticesBefore = vertices.Count;
        int trianglesBefore = triangles.Count;

        double inv = 1.0 / epsilon;
        double epsilonSquared = epsilon * epsilon;
        var voxelToNewIds = new Dictionary<(long X, long Y, long Z), List<int>>();
        var newVertices = new List<RealPoint>(vertices.Count);
        var remap = new int[vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            var p = vertices[i];
            remap[i] = GetOrAddCanonicalId(p, inv, epsilonSquared, newVertices, voxelToNewIds);
        }

        var remappedTris = new List<(int A, int B, int C)>(triangles.Count);
        int collapsedRemoved = 0;

        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            int ra = remap[a];
            int rb = remap[b];
            int rc = remap[c];

            if (ra == rb || rb == rc || rc == ra)
            {
                collapsedRemoved++;
                continue;
            }

            remappedTris.Add((ra, rb, rc));
        }

        vertices.Clear();
        vertices.AddRange(newVertices);

        triangles.Clear();
        triangles.AddRange(remappedTris);

        return new VertexWeldStats(
            verticesBefore,
            vertices.Count,
            trianglesBefore,
            triangles.Count,
            collapsedRemoved);
    }

    public static VertexWeldStats WeldInPlace(
        List<RealPoint> vertices,
        List<(int A, int B, int C)> triangles,
        List<string> provenance,
        double epsilon)
    {
        if (provenance is null) throw new ArgumentNullException(nameof(provenance));
        if (provenance.Count != triangles.Count)
        {
            throw new InvalidOperationException($"Provenance list must match triangle count (prov={provenance.Count}, tris={triangles.Count}).");
        }

        if (vertices.Count == 0 || triangles.Count == 0)
        {
            return new VertexWeldStats(vertices.Count, vertices.Count, triangles.Count, triangles.Count, 0);
        }

        int verticesBefore = vertices.Count;
        int trianglesBefore = triangles.Count;

        double inv = 1.0 / epsilon;
        double epsilonSquared = epsilon * epsilon;
        var voxelToNewIds = new Dictionary<(long X, long Y, long Z), List<int>>();
        var newVertices = new List<RealPoint>(vertices.Count);
        var remap = new int[vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            var p = vertices[i];
            remap[i] = GetOrAddCanonicalId(p, inv, epsilonSquared, newVertices, voxelToNewIds);
        }

        var remappedTris = new List<(int A, int B, int C)>(triangles.Count);
        var remappedProv = new List<string>(provenance.Count);
        int collapsedRemoved = 0;

        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            int ra = remap[a];
            int rb = remap[b];
            int rc = remap[c];

            if (ra == rb || rb == rc || rc == ra)
            {
                collapsedRemoved++;
                continue;
            }

            remappedTris.Add((ra, rb, rc));
            remappedProv.Add(provenance[i]);
        }

        vertices.Clear();
        vertices.AddRange(newVertices);

        triangles.Clear();
        triangles.AddRange(remappedTris);

        provenance.Clear();
        provenance.AddRange(remappedProv);

        return new VertexWeldStats(
            verticesBefore,
            vertices.Count,
            trianglesBefore,
            triangles.Count,
            collapsedRemoved);
    }

    private static int GetOrAddCanonicalId(
        in RealPoint p,
        double inv,
        double epsilonSquared,
        List<RealPoint> idToPosition,
        Dictionary<(long X, long Y, long Z), List<int>> voxelToIds)
    {
        long vx = (long)Math.Floor(p.X * inv);
        long vy = (long)Math.Floor(p.Y * inv);
        long vz = (long)Math.Floor(p.Z * inv);

        for (long dx = -1; dx <= 1; dx++)
        for (long dy = -1; dy <= 1; dy++)
        for (long dz = -1; dz <= 1; dz++)
        {
            var key = (vx + dx, vy + dy, vz + dz);

            if (!voxelToIds.TryGetValue(key, out var candidates))
            {
                continue;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int id = candidates[i];
                var c = idToPosition[id];

                if (p.DistanceSquared(in c) <= epsilonSquared)
                {
                    return id;
                }
            }
        }

        int newId = idToPosition.Count;
        idToPosition.Add(p);

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
