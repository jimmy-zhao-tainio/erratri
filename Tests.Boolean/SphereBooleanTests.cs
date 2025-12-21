using Geometry;
using Boolean;
using World;
using Xunit;

namespace Tests.Boolean;

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

        var realMesh = global::Boolean.Operation.Union(sphereA.Mesh, sphereB.Mesh);
        Assert.NotNull(realMesh);
        Assert.NotEmpty(realMesh.Triangles);

        var snapped = BooleanMeshConverter.ToMesh(realMesh);
        Assert.True(snapped.Count > 0);
    }
}


