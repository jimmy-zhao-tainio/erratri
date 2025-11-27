using Geometry;
using Kernel;
using World;
using Xunit;

namespace Kernel.Tests;

public class CheeseRegressionTests
{
    private static Box MakeBox(long halfSize)
    {
        var box = new Box(width: halfSize * 2, depth: halfSize * 2, height: halfSize * 2);
        box.Position(-halfSize, -halfSize, -halfSize);
        return box;
    }

    [Fact]
    public void BoxMinusSingleCylinder_ShouldProduceMesh()
    {
        long half = 100;
        var box = MakeBox(half);

        var cyl = new Cylinder(radius: 60, thickness: 120, height: 300, center: new Point(0, 0, 0), yTiltDeg: 90);

        var result = BooleanOps.DifferenceAB(box.Mesh, cyl.Mesh);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Triangles);
    }

    [Fact]
    public void BoxMinusCornerSphereAndCylinder_ShouldProduceMesh()
    {
        long half = 100;
        var box = MakeBox(half);

        var cornerCenter = new Point(half, half, half);
        var cornerSphere = new Sphere(radius: half, subdivisions: 3, center: cornerCenter);

        // Cylinder passing near the same corner along Y.
        var cyl = new Cylinder(radius: 60, thickness: 120, height: 300, center: new Point(half, 0, half), xTiltDeg: 90);

        var union = BooleanMeshConverter.ToClosedSurface(BooleanOps.Union(cornerSphere.Mesh, cyl.Mesh));
        var result = BooleanOps.DifferenceAB(box.Mesh, union);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Triangles);
    }
}
