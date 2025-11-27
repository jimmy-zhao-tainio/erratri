using Geometry;
using Kernel;
using World;
using Xunit;

namespace Kernel.Tests;

public class SphereBooleanTests
{
    [Fact]
    public void SphereSphere_SubdivideSingleEdgeToEdge_DoesNotThrow()
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 0, 0);

        var sphereA = new Sphere(r, subdivisions: 3, center: aCenter);
        var sphereB = new Sphere(r, subdivisions: 3, center: bCenter);

        var booleanMesh = BooleanOps.Union(sphereA.Mesh, sphereB.Mesh);
        Assert.NotNull(booleanMesh);
        Assert.NotEmpty(booleanMesh.Triangles);

        var snapped = BooleanMeshConverter.ToClosedSurface(booleanMesh);
        Assert.True(snapped.Count > 0);
    }
}
