using Boolean;
using Geometry;
using Geometry.Topology;
using Xunit;

namespace Tests.Boolean.Operation;

public class IntegerMeshDelaunayTests
{
    [Fact]
    public void ConversionRepairsADiagonalMadeIllegalByRounding()
    {
        // BD is Delaunay here; after rounding, AC is the legal diagonal.
        var real = new RealMesh(new[] { new RealPoint(-.31, -.38, 0), new RealPoint(4.19, .39, 0),
            new RealPoint(4.32, .56, 0), new RealPoint(.27, 4.01, 0) }, new[] { (0, 1, 3), (1, 2, 3) });
        var mesh = BooleanMeshConverter.ToMesh(real);
        Assert.Equal(2, mesh.Count);
        var a = new Point(0, 0, 0); var c = new Point(4, 1, 0);
        Assert.All(mesh.Triangles, t =>
        {
            var vertices = new[] { t.P0, t.P1, t.P2 };
            Assert.Contains(a, vertices); Assert.Contains(c, vertices);
        });
        Assert.Equal(10, mesh.Triangles.Sum(t =>
            ((t.P1.X - t.P0.X) * (t.P2.Y - t.P0.Y) - (t.P1.Y - t.P0.Y) * (t.P2.X - t.P0.X)) / 2.0));
    }

    [Fact]
    public void CreasesArePreservedEvenWhenProjectedDiagonalIsIllegal()
    {
        var p = new[] { new Point(0, 0, 0), new Point(4, 0, 0), new Point(4, 1, 1), new Point(0, 4, 0) };
        var triangles = new List<(int A, int B, int C)> { (0, 1, 3), (1, 2, 3) };
        var original = triangles.ToArray();
        IntegerMeshDelaunay.ConformAndLegalize(p, triangles);
        Assert.Equal(original, triangles);
    }

    [Fact]
    public void PreservesConnectivityWhenTheAlternateDiagonalAlreadyExists()
    {
        // A flattened tetrahedral surface: both diagonals already exist on
        // opposite sides. Flipping BD would merge those distinct surface edges.
        var p = new[] { new Point(0, 0, 0), new Point(4, 0, 0), new Point(4, 1, 0), new Point(0, 4, 0) };
        var triangles = new List<(int A, int B, int C)> { (0, 1, 3), (1, 2, 3), (0, 2, 1), (0, 3, 2) };
        var original = triangles.ToArray();
        IntegerMeshDelaunay.ConformAndLegalize(p, triangles);
        Assert.Equal(original, triangles);
        OperationOrientationTests.AssertOriented(new RealMesh(p.Select(q => new RealPoint(q)).ToArray(), triangles));
    }

    [Fact]
    public void SeamSubdivisionIsExactAndRetainsEveryBoundaryVertex()
    {
        var p = new[] { new Point(0, 0, 0), new Point(4, 0, 0), new Point(0, 4, 0), new Point(2, 0, 0) };
        var triangles = new List<(int A, int B, int C)> { (0, 1, 2) };
        IntegerMeshDelaunay.ConformAndLegalize(p, triangles);
        Assert.Equal(2, triangles.Count);
        Assert.All(triangles, t => Assert.Contains(3, new[] { t.A, t.B, t.C }));
        Assert.Equal(0, IntegerMeshDelaunay.CountFlippableIllegalEdges(p, triangles));
        Assert.All(triangles, t => Assert.True(new IntegerPlane(p[0], p[1], p[2]).Orient(p[t.A], p[t.B], p[t.C]) > 0));
    }
}