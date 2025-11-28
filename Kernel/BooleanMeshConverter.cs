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

            var q0 = RoundPoint(p0);
            var q1 = RoundPoint(p1);
            var q2 = RoundPoint(p2);

            if (IsDegenerate(q0, q1, q2))
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

    private static Point RoundPoint(in RealPoint p)
    {
        long x = (long)Math.Round(p.X);
        long y = (long)Math.Round(p.Y);
        long z = (long)Math.Round(p.Z);
        return new Point(x, y, z);
    }

    private static bool IsDegenerate(Point p0, Point p1, Point p2)
    {
        long v0x = p1.X - p0.X;
        long v0y = p1.Y - p0.Y;
        long v0z = p1.Z - p0.Z;

        long v1x = p2.X - p0.X;
        long v1y = p2.Y - p0.Y;
        long v1z = p2.Z - p0.Z;

        long cx = v0y * v1z - v0z * v1y;
        long cy = v0z * v1x - v0x * v1z;
        long cz = v0x * v1y - v0y * v1x;

        double lenSq = (double)cx * cx + (double)cy * cy + (double)cz * cz;
        const double epsSq = 1e-12;
        return lenSq < epsSq;
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
