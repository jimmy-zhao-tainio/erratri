using Geometry;
using Boolean;
using World;

namespace Demo.Boolean.Mesh.Cheese;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Swiss-cheese cube: box drilled by three orthogonal tunnels with scooped corners.
        var cube = new Box(width: 400, depth: 400, height: 400).Position(-200, -200, -200);

        // Use rectangular prisms to drill robust tunnels along X, Y, Z.
        Shape tunnelX = new Box(width: 600, depth: 200, height: 200).Position(-300, -100, -100);
        Shape tunnelY = new Box(width: 200, depth: 600, height: 200).Position(-100, -300, -100);
        Shape tunnelZ = new Box(width: 200, depth: 200, height: 600).Position(-100, -100, -300);

        Shape drilled = new DifferenceAB(new DifferenceAB(new DifferenceAB(cube, tunnelX), tunnelY), tunnelZ);

        long rCorner = 200;
        long rOffset = 220;
        Shape withCorners = drilled;
        foreach (var sx in new[] { -1, 1 })
        foreach (var sy in new[] { -1, 1 })
        foreach (var sz in new[] { -1, 1 })
        {
            var cornerCenter = new Point(sx * rOffset, sy * rOffset, sz * rOffset);
            var cornerSphere = new Sphere(rCorner, subdivisions: 3, center: cornerCenter);
            withCorners = new DifferenceAB(withCorners, cornerSphere);
        }

        var world = new World.World();
        world.Add(withCorners);

        var outPath = "cheese.stl";
        world.Save(outPath);
        Console.WriteLine($"Demo.Boolean.Mesh.Cheese: wrote {System.IO.Path.GetFullPath(outPath)}");
    }
}
