using ConstrainedTriangulator;
using Geometry;
using Xunit;

namespace Tests.Boolean.Triangulation;

public class IntegerConformingDelaunayTests
{
    [Fact]
    public void RefinesOnlyAtLatticePointsAndCertifiesFinalIntegerTriangles()
    {
        var points = new[] { new Point(0, 0, 0), new Point(4, 2, 0), new Point(2, 0, 0), new Point(2, 2, 0) };
        var result = IntegerConformingDelaunay.Run(points, new[] { (0, 1) });
        Assert.Equal(points, result.Points.Take(4));
        Assert.Contains(new Point(2, 1, 0), result.Points);
        Assert.All(result.Points.Skip(4), p => Assert.True(IntegerPlane.OnSegment(points[0], points[1], p)));
        AssertDelaunay(result);
    }

    [Fact]
    public void PrimitiveNonDelaunayConstraintFailsInsteadOfLeavingTheLattice()
    {
        var points = new[] { new Point(0, 0, 0), new Point(2, 1, 0), new Point(1, 0, 0), new Point(1, 1, 0) };
        var error = Assert.Throws<InvalidOperationException>(() => IntegerConformingDelaunay.Run(points, new[] { (0, 1) }));
        Assert.Contains("no interior lattice point", error.Message);
    }

    [Fact]
    public void RecoversCocircularConstraintsWithoutSteinerPoints()
    {
        var points = new[] { new Point(5, 0, 0), new Point(4, 3, 0), new Point(0, 5, 0), new Point(-4, 3, 0),
            new Point(-5, 0, 0), new Point(-4, -3, 0), new Point(0, -5, 0), new Point(4, -3, 0) };
        for (int a = 0; a < points.Length; a++)
            for (int b = a + 1; b < points.Length; b++)
            {
                var result = IntegerConformingDelaunay.Run(points, new[] { (a, b) }, maxSteinerPoints: 0);
                Assert.Equal(points.Length, result.Points.Count);
                AssertDelaunay(result);
            }
    }

    [Fact]
    public void PhysicalMetricIsUsedOnTiltedPlanes()
    {
        // The XY projection puts d outside; the physical circle on z=2x contains it.
        var a = new Point(0, 0, 0); var b = new Point(4, 0, 8); var c = new Point(4, 1, 8);
        var d = new Point(1, 3, 2);
        var plane = new IntegerPlane(a, b, c);
        Assert.True(plane.InCircle(a, b, c, d) > 0);
        AssertDelaunay(IntegerConformingDelaunay.Run(new[] { a, b, c, d }, Array.Empty<(int, int)>()));
    }

    [Fact]
    public void ExactPredicatesKeepUnitDifferencesBeyondDoublePrecision()
    {
        long n = long.MaxValue - 10;
        var a = new Point(n, n, n); var b = new Point(n + 4, n, n); var c = new Point(n + 4, n + 1, n);
        var d = new Point(n, n + 4, n);
        var plane = new IntegerPlane(a, b, c);
        Assert.Equal(1, plane.Orient(a, b, c));
        Assert.Equal(-1, plane.InCircle(a, b, c, d));
        AssertDelaunay(IntegerConformingDelaunay.Run(new[] { a, b, c, d }, Array.Empty<(int, int)>()));
        Assert.True(IntegerPlane.TrySplit(new Point(long.MinValue, 0, 0), new Point(long.MaxValue, 0, 0), out var split));
        Assert.Equal(-1, split.X);
    }

    [Fact]
    public void IntegerTriangleAreaDoesNotWrapToZero()
    {
        var triangle = Triangle.FromWinding(new Point(0, 0, 0), new Point(65536, 0, 0), new Point(0, 65536, 0));
        Assert.Equal(1, triangle.Normal.Z);
        Assert.True(IntegerPlane.AreCollinear(new Point(long.MinValue, 0, 0), new Point(0, 0, 0), new Point(long.MaxValue, 0, 0)));
    }

    [Fact]
    public void ExistingIntegerCrossingVertexSplitsBothConstraints()
    {
        var p = new[] { new Point(0, 0, 0), new Point(4, 0, 0), new Point(4, 4, 0),
            new Point(0, 4, 0), new Point(2, 2, 0) };
        var result = IntegerConformingDelaunay.Run(p, new[] { (0, 2), (1, 3) }, 0);
        Assert.Equal(5, result.Points.Count);
        Assert.Equal(4, result.Segments.Count);
        Assert.All(result.Segments, e => Assert.True(e.A == 4 || e.B == 4));
        AssertDelaunay(result);
    }

    [Fact]
    public void SeededLatticeSetsPreserveAreaAndPassGlobalEmptyCircleChecks()
    {
        for (int axis = 0; axis < 3; axis++)
            for (int seed = 0; seed < 8; seed++)
            {
                Point Map(long x, long y) => axis == 0 ? new Point(x, y, 0)
                    : axis == 1 ? new Point(x, 0, y) : new Point(0, x, y);
                var random = new Random(seed);
                var points = new HashSet<Point> { Map(0, 0), Map(20, 0), Map(20, 20), Map(0, 20) };
                while (points.Count < 24) points.Add(Map(random.Next(21), random.Next(21)));
                var result = IntegerConformingDelaunay.Run(points.ToArray(), Array.Empty<(int, int)>(), 0);
                AssertDelaunay(result);
                long area2 = 0;
                foreach (var t in result.Triangles)
                {
                    var a = result.Points[t.A]; var b = result.Points[t.B]; var c = result.Points[t.C];
                    long X(Point p) => axis == 2 ? p.Y : p.X;
                    long Y(Point p) => axis == 0 ? p.Y : p.Z;
                    area2 += (X(b) - X(a)) * (Y(c) - Y(a)) - (Y(b) - Y(a)) * (X(c) - X(a));
                }
                Assert.Equal(800, area2);
            }
    }

    [Fact]
    public void InvalidPslgsAndBudgetExhaustionFailExplicitly()
    {
        var points = new[] { new Point(0, 0, 0), new Point(4, 2, 0), new Point(2, 0, 0), new Point(2, 2, 0) };
        Assert.Throws<InvalidOperationException>(() => IntegerConformingDelaunay.Run(points, new[] { (0, 1) }, 0));
        Assert.Throws<ArgumentException>(() => IntegerConformingDelaunay.Run(points, new[] { (0, 1), (2, 3) }));
        Assert.Throws<ArgumentException>(() => IntegerConformingDelaunay.Run(points, new[] { (-1, 1) }));
        Assert.Throws<ArgumentException>(() => IntegerConformingDelaunay.Run(new[] { points[0], points[1], points[2], new Point(0, 0, 1) }, Array.Empty<(int, int)>()));
    }

    private static void AssertDelaunay(IntegerConformingDelaunay.GridResult result)
    {
        var p = result.Points;
        var first = result.Triangles[0];
        var plane = new IntegerPlane(p[first.A], p[first.B], p[first.C]);
        var edges = new HashSet<(int, int)>();
        var used = new HashSet<int>();
        foreach (var t in result.Triangles)
        {
            Assert.True(plane.Orient(p[t.A], p[t.B], p[t.C]) > 0);
            foreach (var q in p) Assert.True(plane.InCircle(p[t.A], p[t.B], p[t.C], q) <= 0);
            foreach (var e in new[] { (t.A, t.B), (t.B, t.C), (t.C, t.A) })
            { edges.Add(e.Item1 < e.Item2 ? e : (e.Item2, e.Item1)); used.Add(e.Item1); }
        }
        Assert.Equal(p.Count, used.Count);
        Assert.All(result.Segments, e => Assert.Contains(e.A < e.B ? e : (e.B, e.A), edges));
    }
}