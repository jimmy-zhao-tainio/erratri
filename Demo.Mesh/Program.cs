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
        var bCenter = new Point(150, 0, 0);

        var a = new Sphere(r, subdivisions: 3, center: aCenter);
        var b = new Sphere(r, subdivisions: 3, center: bCenter);

        // Build boolean shapes and lay them out in a grid.
        var spacing = 500;
        var union = new Union(a, b).Position(0, 0, 0);
        var intersection = new Intersection(a, b).Position(spacing, 0, 0);
        var diffAB = new DifferenceAB(a, b).Position(2 * spacing, 0, 0);
        var diffBA = new DifferenceBA(a, b).Position((int)(2.5 * spacing), 0, 0);

        var world = new World.World();
        world.Add(union);
        world.Add(intersection);
        world.Add(diffAB);
        world.Add(diffBA);

        var outPath = "spheres_boolean_showcase.stl";
        world.Save(outPath);
        Console.WriteLine($"Demo.Mesh: wrote {System.IO.Path.GetFullPath(outPath)}");
    }

}
