using Geometry;
using World;
using Xunit;

namespace Tests.Boolean.Operation;

public class LoftTests
{
    private static Point[] Square(long size, long z) => new[]
    {
        new Point(-size, -size, z), new Point(size, -size, z),
        new Point(size, size, z), new Point(-size, size, z)
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HollowLoftHasExpectedVolumeAndOutwardEdgesInEitherSectionOrder(bool reverse)
    {
        var sections = new[] { new LoftSection(Square(10, 0), Square(8, 0)), new LoftSection(Square(10, 30), Square(8, 30)) };
        var loft = new Loft(reverse ? sections.Reverse() : sections);
        var mesh = global::Boolean.BooleanMeshConverter.FromMesh(loft.Mesh);
        OperationOrientationTests.AssertOriented(mesh);
        Assert.Equal(4320, OperationOrientationTests.Volume(mesh), 6);
    }

    [Fact]
    public void SolidTaperAndBooleanCutHaveExpectedVolumes()
    {
        var taper = new Loft(new[] { new LoftSection(Square(10, 0)), new LoftSection(Square(5, 30)) });
        Assert.Equal(7000, OperationOrientationTests.Volume(global::Boolean.BooleanMeshConverter.FromMesh(taper.Mesh)), 6);
        var shell = new Loft(new[] { new LoftSection(Square(10, 0), Square(8, 0)), new LoftSection(Square(10, 30), Square(8, 30)) });
        var cutter = new Box(40, 40, 10).Translate(-20, -20, -5);
        var cut = new DifferenceAB(shell, cutter);
        var mesh = global::Boolean.BooleanMeshConverter.FromMesh(cut.Mesh);
        OperationOrientationTests.AssertOriented(mesh);
        Assert.Equal(3600, OperationOrientationTests.Volume(mesh), 6);
    }

    [Fact]
    public void RejectsNonplanarConcaveAndInvalidHoleSections()
    {
        var nonplanar = Square(10, 0); nonplanar[3] = new Point(-10, 10, 1);
        Assert.Throws<ArgumentException>(() => new LoftSection(nonplanar));
        var concave = Square(10, 0); concave[2] = new Point(-5, -5, 0);
        Assert.Throws<ArgumentException>(() => new LoftSection(concave));
        Assert.Throws<ArgumentException>(() => new LoftSection(Square(10, 0), Square(11, 0)));
        Assert.Throws<ArgumentException>(() => new LoftSection(Square(10, 0), Square(8, 0).Reverse()));
        Assert.Throws<ArgumentException>(() => new Loft(new[] { new LoftSection(Square(10, 0)), new LoftSection(Square(10, 30), Square(8, 30)) }));
        Assert.Throws<ArgumentException>(() => new Loft(new[] { new LoftSection(Square(10, 0)), new LoftSection(Square(10, 0)) }));
    }
}
