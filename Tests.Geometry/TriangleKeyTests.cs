using Xunit;
using Geometry.Topology;

namespace Tests.Geometry;

public class TriangleKeyTests
{
    [Fact]
    public void Equality_IgnoresOrder()
    {
        var p0 = new Point(0, 0, 0);
        var p1 = new Point(5, 1, -2);
        var p2 = new Point(-3, 4, 7);

        var a = new TriangleKey(p0, p1, p2);
        var b = new TriangleKey(p2, p0, p1);
        var c = new TriangleKey(p1, p2, p0);

        Assert.Equal(a, b);
        Assert.Equal(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(a.GetHashCode(), c.GetHashCode());
    }

    [Fact]
    public void Inequality_DifferentPointSet()
    {
        var p0 = new Point(0, 0, 0);
        var p1 = new Point(1, 0, 0);
        var p2 = new Point(0, 1, 0);
        var q2 = new Point(0, 2, 0);

        var a = new TriangleKey(p0, p1, p2);
        var b = new TriangleKey(p0, p1, q2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void FromTriangle_MatchesFromPoints()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(3, 0, 0);
        var c = new Point(0, 3, 0);
        var d = new Point(0, 0, 4);

        var tet = new Tetrahedron(a, b, c, d);
        var k1 = TriangleKey.FromTriangle(tet.ABC);
        var k2 = TriangleKey.FromPoints(c, a, b);
        Assert.Equal(k1, k2);
    }

    [Fact]
    public void Dictionary_CountsByTriangleKey()
    {
        var p0 = new Point(0, 0, 0);
        var p1 = new Point(1, 0, 0);
        var p2 = new Point(0, 1, 0);
        var p3 = new Point(0, 0, 1);

        var kA = new TriangleKey(p0, p1, p2);
        var kB = new TriangleKey(p1, p2, p0); // same set
        var kC = new TriangleKey(p0, p2, p3); // different

        var map = new Dictionary<TriangleKey, int>();
        Increment(map, kA);
        Increment(map, kB);
        Increment(map, kC);

        Assert.Equal(2, map[kA]);
        Assert.Equal(1, map[kC]);
        Assert.False(map.ContainsKey(new TriangleKey(p0, p1, p3)) && map[new TriangleKey(p0, p1, p3)] != 0);
    }

    private static void Increment(Dictionary<TriangleKey, int> map, TriangleKey key)
    {
        if (map.TryGetValue(key, out var c)) map[key] = c + 1; else map[key] = 1;
    }
}


