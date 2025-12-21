using Xunit;
using Geometry.Predicates;

namespace Tests.Geometry.Predicates;

public class TetrahedronPredicatesTests
{
    [Fact]
    public void SharesFace_IdenticalTetrahedra_True()
    {
        var (a, b, c, d) = UnitBase();
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, c, d);
        Assert.True(TetrahedronPredicates.SharesTriangle(t1, t2));
        Assert.True(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out var fa1, out var fb1));
        Assert.True(TrianglePredicates.IsSame(fa1, fb1));
    }

    [Fact]
    public void SharesFace_SharedFaceABC_True()
    {
        var (a, b, c, d) = UnitBase();
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, c, new Point(0, 0, -2)); // shares ABC
        Assert.True(TetrahedronPredicates.SharesTriangle(t1, t2));
        Assert.True(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out var fa2, out var fb2));
        // Specifically the ABC face should be shared
        var abc1 = PredicatesTestHelpers.FaceByVertices(t1, a, b, c);
        var abc2 = PredicatesTestHelpers.FaceByVertices(t2, a, b, c);
        Assert.True(TrianglePredicates.IsSame(abc1, fa2));
        Assert.True(TrianglePredicates.IsSame(abc2, fb2));
    }

    [Fact]
    public void SharesFace_NoSharedFace_False()
    {
        var t1 = new Tetrahedron(new Point(0, 0, 0), new Point(2, 0, 0), new Point(0, 2, 0), new Point(0, 0, 2));
        var t2 = new Tetrahedron(new Point(10, 0, 0), new Point(10, 2, 0), new Point(12, 0, 0), new Point(10, 0, 2));
        Assert.False(TetrahedronPredicates.SharesTriangle(t1, t2));
        Assert.False(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out _, out _));
    }

    [Fact]
    public void SharesFace_ShareEdgeOnly_False()
    {
        var (a, b, c, d) = UnitBase();
        var t1 = new Tetrahedron(a, b, c, d);
        // Shares edge AB only (E, F are distinct from C, D)
        var e = new Point(2, 1, 0);
        var f = new Point(0, 2, 3);
        var t2 = new Tetrahedron(a, b, e, f);
        Assert.False(TetrahedronPredicates.SharesTriangle(t1, t2));
        Assert.False(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out _, out _));
    }

    [Fact]
    public void SharesFace_SharePointOnly_False()
    {
        var (a, b, c, d) = UnitBase();
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, new Point(5, 1, 0), new Point(-1, 2, 3), new Point(3, -2, 4));
        Assert.False(TetrahedronPredicates.SharesTriangle(t1, t2));
        Assert.False(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out _, out _));
    }

    [Fact]
    public void IsSolid_Empty_False()
    {
        Assert.False(TetrahedronPredicates.IsSolid(new List<Tetrahedron>()));
    }

    [Fact]
    public void IsSolid_Single_False()
    {
        var (a, b, c, d) = UnitBase();
        var list = new List<Tetrahedron> { new Tetrahedron(a, b, c, d) };
        Assert.False(TetrahedronPredicates.IsSolid(list));
    }

    [Fact]
    public void IsSolid_TwoSharing_True()
    {
        var (a, b, c, d) = UnitBase();
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, c, new Point(0, 0, -2));
        var list = new List<Tetrahedron> { t1, t2 };
        Assert.True(TetrahedronPredicates.IsSolid(list));
        Assert.True(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out _, out _));
    }

    [Fact]
    public void IsSolid_ChainSharing_True()
    {
        var (a, b, c, d) = UnitBase();
        var tCenter = new Tetrahedron(a, b, c, d);
        var tABC = new Tetrahedron(a, b, c, new Point(0, 0, -3)); // shares ABC
        var tABD = new Tetrahedron(a, b, d, new Point(0, 3, 0));   // shares ABD
        var list = new List<Tetrahedron> { tCenter, tABC, tABD };
        Assert.True(TetrahedronPredicates.IsSolid(list));
        Assert.True(PredicatesTestHelpers.TryFindSharedFace(tCenter, tABC, out _, out _));
        Assert.True(PredicatesTestHelpers.TryFindSharedFace(tCenter, tABD, out _, out _));
    }

    [Fact]
    public void IsSolid_WithIsolated_False()
    {
        var (a, b, c, d) = UnitBase();
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, c, new Point(0, 0, -2)); // shares with t1
        var isolated = new Tetrahedron(new Point(10, 0, 0), new Point(12, 0, 0), new Point(10, 2, 0), new Point(10, 0, 2));

        var list = new List<Tetrahedron> { t1, t2, isolated };
        Assert.False(TetrahedronPredicates.IsSolid(list));
        Assert.True(PredicatesTestHelpers.TryFindSharedFace(t1, t2, out _, out _));
        Assert.False(PredicatesTestHelpers.TryFindSharedFace(t1, isolated, out _, out _));
    }

    private static (Point A, Point B, Point C, Point D) UnitBase()
        => (new Point(0, 0, 0), new Point(1, 0, 0), new Point(0, 1, 0), new Point(0, 0, 1));
}

