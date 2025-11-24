using System;
using System.Collections.Generic;
using Geometry;
using IO;
using Kernel;
using World;

internal static class Program
{
    private static void Main(string[] args)
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var a = new Sphere(r, subdivisions: 3, center: aCenter);
        var b = new Sphere(r, subdivisions: 3, center: bCenter);

        var set = new IntersectionSet(a.Mesh.Triangles, b.Mesh.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);
        var classification = PatchClassifier.Classify(set, patches);
        var selected = BooleanPatchClassifier.Select(BooleanOperation.Union, classification);
        var mesh = BooleanMeshAssembler.Assemble(selected);

        var triangles = ToTriangles(mesh);
        var outPath = "spheres_union.stl";
        StlWriter.Write(triangles, outPath);
        Console.WriteLine($"Wrote union: {System.IO.Path.GetFullPath(outPath)} with {triangles.Count} triangles");
    }

    private static IReadOnlyList<Triangle> ToTriangles(BooleanMesh mesh)
    {
        var tris = new List<Triangle>(mesh.Triangles.Count);
        foreach (var (a, b, c) in mesh.Triangles)
        {
            var p0 = ToPoint(mesh.Vertices[a]);
            var p1 = ToPoint(mesh.Vertices[b]);
            var p2 = ToPoint(mesh.Vertices[c]);
            if (!IsDegenerate(p0, p1, p2))
            {
                tris.Add(Triangle.FromWinding(p0, p1, p2));
            }
        }
        return tris;
    }

    private static bool IsDegenerate(Point a, Point b, Point c)
    {
        long abx = b.X - a.X;
        long aby = b.Y - a.Y;
        long abz = b.Z - a.Z;
        long acx = c.X - a.X;
        long acy = c.Y - a.Y;
        long acz = c.Z - a.Z;
        long cxp = aby * acz - abz * acy;
        long cyp = abz * acx - abx * acz;
        long czp = abx * acy - aby * acx;
        return cxp == 0 && cyp == 0 && czp == 0;
    }

    private static Point ToPoint(in RealPoint p)
    {
        long x = (long)Math.Round(p.X);
        long y = (long)Math.Round(p.Y);
        long z = (long)Math.Round(p.Z);
        return new Point(x, y, z);
    }
}
