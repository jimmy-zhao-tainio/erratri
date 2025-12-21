using Xunit;
using Geometry.Predicates;

namespace Tests.Geometry.Predicates;

public class TrianglePredicatesTests
{
    [Fact]
    public void IsSame_SameOrder_True()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d1 = new Point(0, 0, 1);
        var d2 = new Point(0, 0, 2);

        var tet1 = new Tetrahedron(a, b, c, d1);
        var tet2 = new Tetrahedron(a, b, c, d2);
        var t1 = PredicatesTestHelpers.FaceByVertices(tet1, a, b, c);
        var t2 = PredicatesTestHelpers.FaceByVertices(tet2, a, b, c);

        Assert.True(TrianglePredicates.IsSame(t1, t2));
        Assert.True(TrianglePredicates.IsSame(t2, t1));
    }

    [Fact]
    public void IsSame_Permutations_True()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(2, 0, 0);
        var c = new Point(0, 2, 0);

        var d1 = new Point(0, 0, 1);
        var d2 = new Point(0, 0, 2);
        var d3 = new Point(0, 0, 3);

        var tetABC = new Tetrahedron(a, b, c, d1);
        var tetBCA = new Tetrahedron(b, c, a, d2);
        var tetCAB = new Tetrahedron(c, a, b, d3);
        var tABC = PredicatesTestHelpers.FaceByVertices(tetABC, a, b, c);
        var tBCA = PredicatesTestHelpers.FaceByVertices(tetBCA, a, b, c);
        var tCAB = PredicatesTestHelpers.FaceByVertices(tetCAB, a, b, c);

        Assert.True(TrianglePredicates.IsSame(tABC, tBCA));
        Assert.True(TrianglePredicates.IsSame(tABC, tCAB));
        Assert.True(TrianglePredicates.IsSame(tBCA, tCAB));
    }

    [Fact]
    public void IsSame_DifferentVertex_False()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d1 = new Point(0, 0, 1);
        var tetABC = new Tetrahedron(a, b, c, d1);
        var tABC = PredicatesTestHelpers.FaceByVertices(tetABC, a, b, c);

        var e = new Point(1, 1, 0);
        var d2 = new Point(0, 0, 2);
        var tetABE = new Tetrahedron(a, b, e, d2);
        var tABE = PredicatesTestHelpers.FaceByVertices(tetABE, a, b, e);

        Assert.False(TrianglePredicates.IsSame(tABC, tABE));
        Assert.False(TrianglePredicates.IsSame(tABE, tABC));
    }

    [Fact]
    public void IsSame_ShareTwoVerticesOnly_False()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d = new Point(0, 0, 1);
        var e = new Point(2, 1, 0);

        var tetABC = new Tetrahedron(a, b, c, d);
        var tetABE = new Tetrahedron(a, b, e, d);
        var tABC = PredicatesTestHelpers.FaceByVertices(tetABC, a, b, c);
        var tABE = PredicatesTestHelpers.FaceByVertices(tetABE, a, b, e);

        Assert.False(TrianglePredicates.IsSame(tABC, tABE));
    }
}

