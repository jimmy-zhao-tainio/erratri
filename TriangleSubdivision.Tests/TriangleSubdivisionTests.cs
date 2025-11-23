using System;
using System.Collections.Generic;
using Geometry;
using Kernel;
using TS = Kernel.TriangleSubdivision;
using Xunit;

namespace TriangleSubdivision.Tests;

public class TriangleSubdivisionTests
{
    [Fact]
    public void Subdivide_NoSegments_ReturnsOriginalTriangle()
    {
        var v0 = new Point(0, 0, 0);
        var v1 = new Point(10, 0, 0);
        var v2 = new Point(0, 10, 0);
        var missing = new Point(0, 0, 1);

        var tri = new Triangle(v0, v1, v2, missing);

        var points = new List<TS.IntersectionPoint>();
        var segments = new List<TS.IntersectionSegment>();

        var patches = TS.Subdivide(in tri, points, segments);

        var single = Assert.Single(patches);

        Assert.Equal(tri.P0.X, single.P0.X);
        Assert.Equal(tri.P0.Y, single.P0.Y);
        Assert.Equal(tri.P0.Z, single.P0.Z);

        Assert.Equal(tri.P1.X, single.P1.X);
        Assert.Equal(tri.P1.Y, single.P1.Y);
        Assert.Equal(tri.P1.Z, single.P1.Z);

        Assert.Equal(tri.P2.X, single.P2.X);
        Assert.Equal(tri.P2.Y, single.P2.Y);
        Assert.Equal(tri.P2.Z, single.P2.Z);
    }

    [Fact]
    public void Subdivide_WithSegments_ThrowsForNow()
    {
        var v0 = new Point(0, 0, 0);
        var v1 = new Point(10, 0, 0);
        var v2 = new Point(0, 10, 0);
        var missing = new Point(0, 0, 1);

        var tri = new Triangle(v0, v1, v2, missing);

        var points = new List<TS.IntersectionPoint>
        {
            new TS.IntersectionPoint(
                new Barycentric(0.5, 0.5, 0.0),
                new RealPoint(5.0, 5.0, 0.0))
        };

        var segments = new List<TS.IntersectionSegment>
        {
            new TS.IntersectionSegment(0, 0)
        };

        Assert.Throws<InvalidOperationException>(
            () => TS.Subdivide(in tri, points, segments));
    }

    [Fact]
    public void Subdivide_SingleEdgeToEdge_SplitsTriangleAndPreservesArea()
    {
        // Simple right triangle in the XY plane.
        var v0 = new Point(0, 0, 0);
        var v1 = new Point(10, 0, 0);
        var v2 = new Point(0, 10, 0);
        var missing = new Point(0, 0, 1);

        var tri = new Triangle(v0, v1, v2, missing);

        // P on edge V0-V1 (Edge0).
        var baryP = new Barycentric(0.75, 0.25, 0.0);
        var posP = tri.FromBarycentric(in baryP);

        // Q on edge V1-V2 (Edge1).
        var baryQ = new Barycentric(0.0, 0.7, 0.3);
        var posQ = tri.FromBarycentric(in baryQ);

        var points = new List<TS.IntersectionPoint>
        {
            new TS.IntersectionPoint(baryP, posP),
            new TS.IntersectionPoint(baryQ, posQ)
        };

        var segments = new List<TS.IntersectionSegment>
        {
            new TS.IntersectionSegment(0, 1)
        };

        var patches = TS.Subdivide(in tri, points, segments);

        Assert.Equal(3, patches.Count);

        var originalArea = TriangleArea(
            new RealPoint(v0),
            new RealPoint(v1),
            new RealPoint(v2));

        double sumArea = 0.0;
        foreach (var patch in patches)
        {
            sumArea += TriangleArea(patch.P0, patch.P1, patch.P2);
        }

        var diff = System.Math.Abs(originalArea - sumArea);
        Assert.True(diff <= 1e-6 * originalArea);

        // The segment P-Q must appear as an edge in at least two patches.
        int edgeCount = 0;
        foreach (var patch in patches)
        {
            if (HasEdge(patch, posP, posQ))
            {
                edgeCount++;
            }
        }

        Assert.True(edgeCount >= 2);
    }

    [Fact]
    public void Subdivide_SingleEdgeToEdge_VertexEndpoint_Throws()
    {
        // Simple right triangle in the XY plane.
        var v0 = new Point(0, 0, 0);
        var v1 = new Point(1, 0, 0);
        var v2 = new Point(0, 1, 0);
        var missing = new Point(0, 0, 1);

        var tri = new Triangle(v0, v1, v2, missing);

        // P exactly at vertex V0.
        var baryP = new Barycentric(1.0, 0.0, 0.0);
        var posP = tri.FromBarycentric(in baryP);

        // Q on edge V1-V2 (Edge1), away from vertices.
        var baryQ = new Barycentric(0.0, 0.4, 0.6);
        var posQ = tri.FromBarycentric(in baryQ);

        var points = new List<TS.IntersectionPoint>
        {
            new TS.IntersectionPoint(baryP, posP),
            new TS.IntersectionPoint(baryQ, posQ)
        };

        var segments = new List<TS.IntersectionSegment>
        {
            new TS.IntersectionSegment(0, 1)
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => TS.Subdivide(in tri, points, segments));

        Assert.Contains("vertex endpoints", ex.Message);
    }

    private static double TriangleArea(RealPoint a, RealPoint b, RealPoint c)
    {
        double abx = b.X - a.X;
        double aby = b.Y - a.Y;
        double abz = b.Z - a.Z;

        double acx = c.X - a.X;
        double acy = c.Y - a.Y;
        double acz = c.Z - a.Z;

        // Area = 0.5 * |AB x AC|.
        double cxp = aby * acz - abz * acy;
        double cyp = abz * acx - abx * acz;
        double czp = abx * acy - aby * acx;

        double len = System.Math.Sqrt(cxp * cxp + cyp * cyp + czp * czp);
        return 0.5 * len;
    }

    private static bool HasEdge(
        RealTriangle patch,
        RealPoint a,
        RealPoint b,
        double tol = 1e-6)
    {
        return SameEdge(patch.P0, patch.P1, a, b, tol) ||
               SameEdge(patch.P1, patch.P2, a, b, tol) ||
               SameEdge(patch.P2, patch.P0, a, b, tol);
    }

    private static bool SameEdge(
        RealPoint p0, RealPoint p1,
        RealPoint a, RealPoint b,
        double tol)
    {
        return (SamePoint(p0, a, tol) && SamePoint(p1, b, tol)) ||
               (SamePoint(p0, b, tol) && SamePoint(p1, a, tol));
    }

    private static bool SamePoint(RealPoint p, RealPoint q, double tol)
    {
        double dx = p.X - q.X;
        double dy = p.Y - q.Y;
        double dz = p.Z - q.Z;
        double d2 = dx * dx + dy * dy + dz * dz;
        return d2 <= tol * tol;
    }

    [Fact]
    public void ClassifyEdge_IdentifiesEdgesAndInterior()
    {
        // Exact edge points.
        var onEdge0 = new Barycentric(0.5, 0.5, 0.0); // w = 0
        var onEdge1 = new Barycentric(0.0, 0.5, 0.5); // u = 0
        var onEdge2 = new Barycentric(0.5, 0.0, 0.5); // v = 0

        Assert.Equal(TS.EdgeLocation.Edge0, TS.ClassifyEdge(onEdge0));
        Assert.Equal(TS.EdgeLocation.Edge1, TS.ClassifyEdge(onEdge1));
        Assert.Equal(TS.EdgeLocation.Edge2, TS.ClassifyEdge(onEdge2));

        // Interior point.
        var interior = new Barycentric(0.2, 0.3, 0.5);
        Assert.Equal(TS.EdgeLocation.Interior, TS.ClassifyEdge(interior));
    }

    [Fact]
    public void ClassifyPattern_NoneAndSingleEdgeToEdge()
    {
        var points = new List<TS.IntersectionPoint>();
        var segments = new List<TS.IntersectionSegment>();

        // No segments -> None.
        var pattern = TS.ClassifyPattern(points, segments);
        Assert.Equal(TS.PatternKind.None, pattern);

        // Single edge-to-edge segment: endpoints on distinct edges.
        points.Add(new TS.IntersectionPoint(
            new Barycentric(0.5, 0.5, 0.0), new RealPoint(5.0, 5.0, 0.0))); // Edge0
        points.Add(new TS.IntersectionPoint(
            new Barycentric(0.0, 0.5, 0.5), new RealPoint(5.0, 5.0, 0.0))); // Edge1

        segments.Add(new TS.IntersectionSegment(0, 1));

        pattern = TS.ClassifyPattern(points, segments);
        Assert.Equal(TS.PatternKind.SingleEdgeToEdge, pattern);
    }

    [Fact]
    public void ClassifyPattern_InteriorOrMultipleSegments_IsOther()
    {
        var points = new List<TS.IntersectionPoint>
        {
            new TS.IntersectionPoint(
                new Barycentric(0.2, 0.3, 0.5), new RealPoint(2.0, 3.0, 0.0)), // interior
            new TS.IntersectionPoint(
                new Barycentric(0.5, 0.5, 0.0), new RealPoint(5.0, 5.0, 0.0))  // edge0
        };

        // Single segment with an interior endpoint -> Other.
        var segments = new List<TS.IntersectionSegment>
        {
            new TS.IntersectionSegment(0, 1)
        };

        var pattern = TS.ClassifyPattern(points, segments);
        Assert.Equal(TS.PatternKind.Other, pattern);

        // Multiple segments -> Other.
        segments.Add(new TS.IntersectionSegment(1, 1));
        pattern = TS.ClassifyPattern(points, segments);
        Assert.Equal(TS.PatternKind.Other, pattern);
    }
}
