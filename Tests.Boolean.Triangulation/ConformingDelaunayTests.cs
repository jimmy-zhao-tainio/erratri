using System;
using System.Linq;
using Geometry;
using ConstrainedTriangulator;
using Xunit;

namespace Tests.Boolean.Triangulation;

public class ConformingDelaunayTests
{
    private static readonly RealPoint2D[] Points = { new(0, 0), new(4, 0), new(4, 1), new(0, 4) };

    [Fact]
    public void UnconstrainedDiagonalSatisfiesEmptyCircle()
    {
        var result = ConformingDelaunay.Run(new Input(Points, new[] { (0, 1), (1, 2), (2, 3), (3, 0) }));
        Assert.Equal(4, result.Points.Count);
        AssertEmptyCircles(result);
    }

    [Fact]
    public void NonDelaunayConstraintIsSubdividedWithSteinerVertices()
    {
        var result = ConformingDelaunay.Run(new Input(Points, new[] { (0, 1), (1, 2), (2, 3), (3, 0), (1, 3) }));
        Assert.True(result.Points.Count > 4);
        AssertEmptyCircles(result);
        Assert.All(result.Segments, e => Assert.Contains(result.Triangles, t =>
            new[] { t.A, t.B, t.C }.Contains(e.A) && new[] { t.A, t.B, t.C }.Contains(e.B)));
        double length = result.Segments.Where(e => OnDiagonal(result.Points[e.A]) && OnDiagonal(result.Points[e.B]))
            .Sum(e => Math.Sqrt(Math.Pow(result.Points[e.A].X - result.Points[e.B].X, 2) + Math.Pow(result.Points[e.A].Y - result.Points[e.B].Y, 2)));
        Assert.Equal(Math.Sqrt(32), length, 10);
    }

    [Fact]
    public void SteinerBudgetFailureDoesNotReturnInvalidResult()
    {
        Assert.Throws<InvalidOperationException>(() => ConformingDelaunay.Run(new Input(Points, new[] { (1, 3) }), 0));
    }

    [Fact]
    public void PointsWithoutConstraintsAreTriangulated()
    {
        var result = ConformingDelaunay.Run(new Input(Points, Array.Empty<(int, int)>()));
        Assert.Equal(2, result.Triangles.Count);
        AssertEmptyCircles(result);
    }

    [Fact]
    public void SeededPointSetsPreserveCoverageConstraintsAndEmptyCircles()
    {
        for (int seed = 0; seed < 8; seed++)
        {
            var random = new Random(seed);
            var points = new System.Collections.Generic.List<RealPoint2D>
                { new(0, 0), new(20, 0), new(20, 20), new(0, 20) };
            while (points.Count < 20)
            {
                var p = new RealPoint2D(random.Next(1, 20), random.Next(1, 20));
                if (!points.Any(q => q.X == p.X && q.Y == p.Y)) points.Add(p);
            }
            var result = ConformingDelaunay.Run(new Input(points,
                new[] { (0, 1), (1, 2), (2, 3), (3, 0), (0, 2) }));
            AssertEmptyCircles(result);
            var used = result.Triangles.SelectMany(t => new[] { t.A, t.B, t.C }).ToHashSet();
            Assert.Equal(result.Points.Count, used.Count);
            double area = result.Triangles.Sum(t =>
            {
                var a = result.Points[t.A]; var b = result.Points[t.B]; var c = result.Points[t.C];
                return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) / 2;
            });
            Assert.Equal(400, area, 8);
            Assert.All(result.Segments, e => Assert.Contains(result.Triangles, t =>
                new[] { t.A, t.B, t.C }.Contains(e.A) && new[] { t.A, t.B, t.C }.Contains(e.B)));
        }
    }

    private static bool OnDiagonal(RealPoint2D p) => Math.Abs(p.X + p.Y - 4) < 1e-10;
    private static void AssertEmptyCircles(Result r)
    {
        foreach (var t in r.Triangles)
            foreach (int i in Enumerable.Range(0, r.Points.Count).Except(new[] { t.A, t.B, t.C }))
            {
                var d = r.Points[i]; var a = r.Points[t.A]; var b = r.Points[t.B]; var c = r.Points[t.C];
                double ax = a.X - d.X, ay = a.Y - d.Y, bx = b.X - d.X, by = b.Y - d.Y, cx = c.X - d.X, cy = c.Y - d.Y;
                double det = (ax * ax + ay * ay) * (bx * cy - by * cx) - (bx * bx + by * by) * (ax * cy - ay * cx) + (cx * cx + cy * cy) * (ax * by - ay * bx);
                Assert.True(det <= 1e-9, $"Vertex {i} is inside circumcircle of {t}: {det}");
            }
    }
}