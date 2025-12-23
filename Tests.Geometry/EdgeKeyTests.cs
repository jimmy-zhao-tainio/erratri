using Xunit;
using Geometry.Topology;

namespace Tests.Geometry;

public class EdgeKeyTests
{
    [Fact]
    public void Equality_IgnoresDirection()
    {
        var p = new Point(1, 2, 3);
        var q = new Point(-4, 5, 6);
        var a = new EdgeKey(p, q);
        var b = new EdgeKey(q, p);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Inequality_DifferentEndpoints()
    {
        var p = new Point(0, 0, 0);
        var q = new Point(1, 0, 0);
        var r = new Point(0, 1, 0);
        Assert.NotEqual(new EdgeKey(p, q), new EdgeKey(p, r));
    }

    [Fact]
    public void Dictionary_CountsSharedEdge()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(2, 0, 0);
        var c = new Point(0, 2, 0);
        var d = new Point(0, 0, 2);
        var e = new Point(2, 2, 0);

        var t1 = new Tetrahedron(a, b, c, d).ABC; // edge (a,b)
        var t2 = new Tetrahedron(a, b, e, d).ABD; // shares edge (a,b)

        var map = new Dictionary<EdgeKey, int>();
        Inc(map, new EdgeKey(t1.P0, t1.P1));
        Inc(map, new EdgeKey(t2.P0, t2.P1));

        Assert.Equal(2, map[new EdgeKey(a, b)]);
    }

    private static void Inc(Dictionary<EdgeKey, int> m, EdgeKey k)
    { 
        if (m.TryGetValue(k, out var c)) m[k] = c + 1; else m[k] = 1; 
    }
}


