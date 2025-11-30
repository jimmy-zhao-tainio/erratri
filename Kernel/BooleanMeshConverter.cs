using System;
using System.Collections.Generic;
using Geometry;
using Topology;

namespace Kernel;

public static class BooleanMeshConverter
{
    public static Mesh ToMesh(RealMesh realMesh)
    {
        if (realMesh is null) throw new ArgumentNullException(nameof(realMesh));

        var triangles = new List<Triangle>(realMesh.Triangles.Count);
        for (int i = 0; i < realMesh.Triangles.Count; i++)
        {
            var (a, b, c) = realMesh.Triangles[i];
            var p0 = realMesh.Vertices[a];
            var p1 = realMesh.Vertices[b];
            var p2 = realMesh.Vertices[c];

            var q0 = GridRounding.Snap(p0);
            var q1 = GridRounding.Snap(p1);
            var q2 = GridRounding.Snap(p2);

            if (Triangle.HasZeroArea(in q0, in q1, in q2))
            {
                continue;
            }

            triangles.Add(Triangle.FromWinding(q0, q1, q2));
        }

        return new Mesh(triangles);
    }

    public static RealMesh FromMesh(Mesh mesh)
    {
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));

        var vertices = new List<RealPoint>();
        var map = new Dictionary<Point, int>();
        var tris = new List<(int A, int B, int C)>(mesh.Triangles.Count);

        foreach (var tri in mesh.Triangles)
        {
            int i0 = GetOrAdd(vertices, map, tri.P0);
            int i1 = GetOrAdd(vertices, map, tri.P1);
            int i2 = GetOrAdd(vertices, map, tri.P2);
            tris.Add((i0, i1, i2));
        }

        return new RealMesh(vertices, tris);
    }

    private static int GetOrAdd(List<RealPoint> vertices, Dictionary<Point, int> map, Point p)
    {
        if (map.TryGetValue(p, out var idx))
        {
            return idx;
        }

        vertices.Add(new RealPoint(p.X, p.Y, p.Z));
        idx = vertices.Count - 1;
        map[p] = idx;
        return idx;
    }
}
