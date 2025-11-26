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

        var triangles = ToTriangles(mesh, out int removed);
        var outPath = "spheres_union.stl";
        StlWriter.Write(triangles, outPath);
        Console.WriteLine($"Demo.Mesh: removed {removed} degenerate triangles, kept {triangles.Count}.");
        Console.WriteLine($"Wrote union: {System.IO.Path.GetFullPath(outPath)} with {triangles.Count} triangles");
    }

    private static IReadOnlyList<Triangle> ToTriangles(BooleanMesh mesh, out int removed)
    {
        removed = 0;
        var tris = new List<Triangle>(mesh.Triangles.Count);
        foreach (var (a, b, c) in mesh.Triangles)
        {
            var p0 = ToPoint(mesh.Vertices[a]);
            var p1 = ToPoint(mesh.Vertices[b]);
            var p2 = ToPoint(mesh.Vertices[c]);
            if (TriangleCleaning.IsDegenerate(p0, p1, p2))
            {
                removed++;
                continue;
            }
            tris.Add(Triangle.FromWinding(p0, p1, p2));
        }
        return tris;
    }
    private static Point ToPoint(in RealPoint p)
    {
        long x = (long)Math.Round(p.X);
        long y = (long)Math.Round(p.Y);
        long z = (long)Math.Round(p.Z);
        return new Point(x, y, z);
    }
}

internal static class TriangleCleaning
{
    public static bool IsDegenerate(Point p0, Point p1, Point p2)
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
}
