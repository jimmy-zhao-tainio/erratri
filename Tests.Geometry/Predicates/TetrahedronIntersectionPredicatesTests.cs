using Xunit;
using Geometry.Predicates;

namespace Tests.Geometry.Predicates;

public class TetrahedronIntersectionPredicatesTests
{
    [Fact]
    public void Tetrahedra_FarApart_NoIntersect()
    {
        var t1 = new Tetrahedron(new Point(0,0,0), new Point(5,0,0), new Point(0,5,0), new Point(0,0,5));
        var t2 = new Tetrahedron(new Point(100,0,0), new Point(105,0,0), new Point(100,5,0), new Point(100,0,5));
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }

    [Fact]
    public void Tetrahedron_VertexInside_Other_Intersect()
    {
        var outer = new Tetrahedron(new Point(0,0,0), new Point(10,0,0), new Point(0,10,0), new Point(0,0,10));
        var inner = new Tetrahedron(new Point(1,1,1), new Point(2,1,1), new Point(1,2,1), new Point(1,1,2));
        Assert.True(TetrahedronIntersectionPredicates.Intersects(outer, inner));
    }

    [Fact]
    public void SharedFace_IsContact_NotIntersection()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(2, 0, 0);
        var c = new Point(0, 2, 0);
        var d = new Point(0, 0, 1);   // above z=0
        var e = new Point(0, 0, -1);  // below z=0
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, c, e);
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }

    [Fact]
    public void SharedEdge_IsContact_NotIntersection()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(2, 0, 0);
        var c = new Point(0, 2, 0);
        var d = new Point(0, 0, 2);
        var e = new Point(0, -2, 0);
        var f = new Point(0, 0, -2);
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, e, f); // shares only edge (a,b)
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }

    [Fact]
    public void SharedVertex_IsContact_NotIntersection()
    {
        var a = new Point(0, 0, 0);
        var t1 = new Tetrahedron(a, new Point(1, 0, 0), new Point(0, 1, 0), new Point(0, 0, 1));
        var t2 = new Tetrahedron(a, new Point(-1, 0, 0), new Point(0, -1, 0), new Point(0, 0, -1));
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }
}

