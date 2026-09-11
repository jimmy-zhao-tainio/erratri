using Geometry;
using World;
using Xunit;

namespace Tests.Boolean.Operation;

public class ShapeTransformTests
{
    [Fact]
    public void ChainedTransformsApplyInCallOrderAndKeepTheWorldReference()
    {
        var shape = new Box(10, 20, 30);
        var world = new global::World.World();
        world.Add(shape);
        var original = shape.Mesh.Triangles.SelectMany(t => new[] { t.P0, t.P1, t.P2 }).ToHashSet();

        var result = shape.Translate(100, 0, 0).Rotate(zDegrees: 90).Translate(0, 5, 0);

        Assert.Same(shape, result);
        Assert.Same(shape, world.Shapes[0]);
        var actual = result.Mesh.Triangles.SelectMany(t => new[] { t.P0, t.P1, t.P2 }).ToHashSet();
        Assert.True(actual.SetEquals(original.Select(p => new Point(-p.Y, p.X + 105, p.Z))));
        var mesh = global::Boolean.BooleanMeshConverter.FromMesh(result.Mesh);
        OperationOrientationTests.AssertOriented(mesh);
        Assert.Equal(6000, OperationOrientationTests.Volume(mesh), 6);
    }

    [Fact]
    public void PositionRemainsRelativeAndEmptyShapesCanBeChained()
    {
        var shape = new Box(10, 20, 30);
        Assert.Same(shape, shape.Position(10, 0, 0).Translate(5, 0, 0));
        Assert.Equal(15L, shape.Mesh.Triangles.Min(t => Math.Min(t.P0.X, Math.Min(t.P1.X, t.P2.X))));
        var empty = new EmptyShape();
        Assert.Same(empty, empty.Translate(1, 2, 3).Rotate(zDegrees: 90));
        Assert.Empty(empty.Mesh.Triangles);
    }

    private sealed class EmptyShape : Shape { }
}
