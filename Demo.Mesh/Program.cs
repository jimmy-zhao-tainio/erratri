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

        var ops = new[]
        {
            BooleanOperation.Union,
            BooleanOperation.Intersection,
            BooleanOperation.DifferenceAB,
            BooleanOperation.DifferenceBA,
            BooleanOperation.SymmetricDifference
        };

        foreach (var op in ops)
        {
            var selected = BooleanPatchClassifier.Select(op, classification);
            var mesh = BooleanMeshAssembler.Assemble(selected);

            var outPath = $"spheres_{op.ToString().ToLower()}.stl";
            WriteMeshStl(mesh, outPath);
            Console.WriteLine($"Demo.Mesh ({op}): wrote {System.IO.Path.GetFullPath(outPath)} with {mesh.Triangles.Count} triangles");
        }
    }

    private static void WriteMeshStl(BooleanMesh mesh, string path)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        var header = new byte[80];
        var tag = System.Text.Encoding.ASCII.GetBytes("Seaharp STL");
        Array.Copy(tag, header, Math.Min(header.Length, tag.Length));
        bw.Write(header);

        bw.Write((uint)mesh.Triangles.Count);
        foreach (var (a, b, c) in mesh.Triangles)
        {
            var p0 = ToPoint(mesh.Vertices[a]);
            var p1 = ToPoint(mesh.Vertices[b]);
            var p2 = ToPoint(mesh.Vertices[c]);

            var n = ComputeNormal(mesh.Vertices[a], mesh.Vertices[b], mesh.Vertices[c]);

            bw.Write((float)n.X);
            bw.Write((float)n.Y);
            bw.Write((float)n.Z);
            bw.Write((float)p0.X); bw.Write((float)p0.Y); bw.Write((float)p0.Z);
            bw.Write((float)p1.X); bw.Write((float)p1.Y); bw.Write((float)p1.Z);
            bw.Write((float)p2.X); bw.Write((float)p2.Y); bw.Write((float)p2.Z);
            bw.Write((ushort)0);
        }
    }
    private static Point ToPoint(in RealPoint p)
    {
        long x = (long)Math.Round(p.X);
        long y = (long)Math.Round(p.Y);
        long z = (long)Math.Round(p.Z);
        return new Point(x, y, z);
    }

    private static RealPoint ComputeNormal(in RealPoint p0, in RealPoint p1, in RealPoint p2)
    {
        double v0x = p1.X - p0.X;
        double v0y = p1.Y - p0.Y;
        double v0z = p1.Z - p0.Z;

        double v1x = p2.X - p0.X;
        double v1y = p2.Y - p0.Y;
        double v1z = p2.Z - p0.Z;

        double nx = v0y * v1z - v0z * v1y;
        double ny = v0z * v1x - v0x * v1z;
        double nz = v0x * v1y - v0y * v1x;
        double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (len <= 0.0)
        {
            return new RealPoint(0, 0, 0);
        }
        double inv = 1.0 / len;
        return new RealPoint(nx * inv, ny * inv, nz * inv);
    }
}
