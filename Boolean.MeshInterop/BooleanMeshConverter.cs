using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Topology;

namespace Boolean;

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

        // From this boundary onward, positions and geometry decisions stay on Z³.
        var points = new List<Point>();
        var indices = new Dictionary<Point, int>();
        int Index(Point p)
        {
            if (!indices.TryGetValue(p, out int index))
            { index = points.Count; points.Add(p); indices.Add(p, index); }
            return index;
        }
        var faces = new List<(int A, int B, int C)>();
        foreach (var triangle in triangles)
            faces.Add((Index(triangle.P0), Index(triangle.P1), Index(triangle.P2)));
        IntegerMeshDelaunay.ConformAndLegalize(points, faces);
        triangles.Clear();
        foreach (var (a, b, c) in faces)
            triangles.Add(Triangle.FromWinding(points[a], points[b], points[c]));
        return new Mesh(triangles);
    }

    public static RealMesh FromMesh(Mesh mesh)
    {
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));

        var vertices = new List<RealPoint>();
        var map = new Dictionary<Point, int>();
        var triangles = new List<(int A, int B, int C)>(mesh.Triangles.Count);

        foreach (var triangle in mesh.Triangles)
        {
            int i0 = GetOrAdd(vertices, map, triangle.P0);
            int i1 = GetOrAdd(vertices, map, triangle.P1);
            int i2 = GetOrAdd(vertices, map, triangle.P2);
            triangles.Add((i0, i1, i2));
        }

        return new RealMesh(vertices, triangles);
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