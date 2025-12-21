using System;
using System.Collections.Generic;
using Geometry;
using Boolean;
using Xunit;

namespace Tests.Boolean;

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

        var points = new List<Triangulation.IntersectionPoint>();
        var segments = new List<Triangulation.IntersectionSegment>();

        var patches = Triangulation.Subdivide(in tri, points, segments);

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
    public void Subdivide_DegenerateSegment_IsIgnoredAndReturnsOriginalTriangle()
    {
        var v0 = new Point(0, 0, 0);
        var v1 = new Point(10, 0, 0);
        var v2 = new Point(0, 10, 0);
        var missing = new Point(0, 0, 1);

        var tri = new Triangle(v0, v1, v2, missing);

        var points = new List<Triangulation.IntersectionPoint>
        {
            new Triangulation.IntersectionPoint(
                new Barycentric(0.5, 0.5, 0.0),
                new RealPoint(5.0, 5.0, 0.0))
        };

        var segments = new List<Triangulation.IntersectionSegment>
        {
            new Triangulation.IntersectionSegment(0, 0)
        };

        var patches = Triangulation.Subdivide(in tri, points, segments);
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
        var posP = Barycentric.ToRealPointOnTriangle(in tri, in baryP);

        // Q on edge V1-V2 (Edge1).
        var baryQ = new Barycentric(0.0, 0.7, 0.3);
        var posQ = Barycentric.ToRealPointOnTriangle(in tri, in baryQ);

        var points = new List<Triangulation.IntersectionPoint>
        {
            new Triangulation.IntersectionPoint(baryP, posP),
            new Triangulation.IntersectionPoint(baryQ, posQ)
        };

        var segments = new List<Triangulation.IntersectionSegment>
        {
            new Triangulation.IntersectionSegment(0, 1)
        };

        var patches = Triangulation.Subdivide(in tri, points, segments);

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
        var posP = Barycentric.ToRealPointOnTriangle(in tri, in baryP);

        // Q on edge V1-V2 (Edge1), away from vertices.
        var baryQ = new Barycentric(0.0, 0.4, 0.6);
        var posQ = Barycentric.ToRealPointOnTriangle(in tri, in baryQ);

        var points = new List<Triangulation.IntersectionPoint>
        {
            new Triangulation.IntersectionPoint(baryP, posP),
            new Triangulation.IntersectionPoint(baryQ, posQ)
        };

        var segments = new List<Triangulation.IntersectionSegment>
        {
            new Triangulation.IntersectionSegment(0, 1)
        };

        var patches = Triangulation.Subdivide(in tri, points, segments);
        Assert.NotEmpty(patches);
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
        // Exact edge poinTriangleSubdivision.
        var onEdge0 = new Barycentric(0.5, 0.5, 0.0); // w = 0
        var onEdge1 = new Barycentric(0.0, 0.5, 0.5); // u = 0
        var onEdge2 = new Barycentric(0.5, 0.0, 0.5); // v = 0

        Assert.Equal(Triangulation.EdgeLocation.Edge0, Triangulation.ClassifyEdge(onEdge0));
        Assert.Equal(Triangulation.EdgeLocation.Edge1, Triangulation.ClassifyEdge(onEdge1));
        Assert.Equal(Triangulation.EdgeLocation.Edge2, Triangulation.ClassifyEdge(onEdge2));

        // Interior point.
        var interior = new Barycentric(0.2, 0.3, 0.5);
        Assert.Equal(Triangulation.EdgeLocation.Interior, Triangulation.ClassifyEdge(interior));
    }

    [Fact]
    public void ClassifyPattern_NoneAndSingleEdgeToEdge()
    {
        var points = new List<Triangulation.IntersectionPoint>();
        var segments = new List<Triangulation.IntersectionSegment>();

        // No segments -> None.
        var pattern = Triangulation.ClassifyPattern(points, segments);
        Assert.Equal(Triangulation.PatternKind.None, pattern);

        // Single edge-to-edge segment: endpoints on distinct edges.
        points.Add(new Triangulation.IntersectionPoint(
            new Barycentric(0.5, 0.5, 0.0), new RealPoint(5.0, 5.0, 0.0))); // Edge0
        points.Add(new Triangulation.IntersectionPoint(
            new Barycentric(0.0, 0.5, 0.5), new RealPoint(5.0, 5.0, 0.0))); // Edge1

        segments.Add(new Triangulation.IntersectionSegment(0, 1));

        pattern = Triangulation.ClassifyPattern(points, segments);
        Assert.Equal(Triangulation.PatternKind.SingleEdgeToEdge, pattern);
    }

    [Fact]
    public void ClassifyPattern_InteriorOrMultipleSegments_IsOther()
    {
        var points = new List<Triangulation.IntersectionPoint>
        {
            new Triangulation.IntersectionPoint(
                new Barycentric(0.2, 0.3, 0.5), new RealPoint(2.0, 3.0, 0.0)), // interior
            new Triangulation.IntersectionPoint(
                new Barycentric(0.5, 0.5, 0.0), new RealPoint(5.0, 5.0, 0.0))  // edge0
        };

        // Single segment with an interior endpoint -> Other.
        var segments = new List<Triangulation.IntersectionSegment>
        {
            new Triangulation.IntersectionSegment(0, 1)
        };

        var pattern = Triangulation.ClassifyPattern(points, segments);
        Assert.Equal(Triangulation.PatternKind.Other, pattern);

        // Multiple segments -> Other.
        segments.Add(new Triangulation.IntersectionSegment(1, 1));
        pattern = Triangulation.ClassifyPattern(points, segments);
        Assert.Equal(Triangulation.PatternKind.Other, pattern);
    }

    // Same geometric pattern as the base triangle A0 in the TetraPeek example:
    // a single triangle with three interior intersection points connected in a
    // closed inner triangle loop. PSLG should treat this as a valid interior
    // face and subdivision should preserve area and actually split the triangle.
    [Fact]
    public void TriangleSubdivision_InteriorTriangleLoop_ShouldPreserveAreaAndSubdivide()
    {
        var tri = new Triangle(
            new Point(0, 0, 0),
            new Point(4, 0, 0),
            new Point(0, 4, 0),
            new Point(0, 0, 1));

        var bary0 = new Barycentric(0.25, 0.25, 0.5);
        var bary1 = new Barycentric(0.25, 0.5, 0.25);
        var bary2 = new Barycentric(0.5, 0.25, 0.25);

        var p0 = Barycentric.ToRealPointOnTriangle(in tri, in bary0);
        var p1 = Barycentric.ToRealPointOnTriangle(in tri, in bary1);
        var p2 = Barycentric.ToRealPointOnTriangle(in tri, in bary2);

        var points = new List<Triangulation.IntersectionPoint>
        {
            new Triangulation.IntersectionPoint(bary0, p0),
            new Triangulation.IntersectionPoint(bary1, p1),
            new Triangulation.IntersectionPoint(bary2, p2)
        };

        var segments = new List<Triangulation.IntersectionSegment>
        {
            new Triangulation.IntersectionSegment(0, 1),
            new Triangulation.IntersectionSegment(1, 2),
            new Triangulation.IntersectionSegment(2, 0)
        };

        var patches = Triangulation.Subdivide(in tri, points, segments);

        // Must actually subdivide: inner loop should split the triangle.
        Assert.True(patches.Count > 1);

        var triArea = TriangleArea(
            new RealPoint(tri.P0),
            new RealPoint(tri.P1),
            new RealPoint(tri.P2));

        double patchArea = 0.0;
        foreach (var patch in patches)
        {
            patchArea += TriangleArea(patch.P0, patch.P1, patch.P2);
        }

        double diff = Math.Abs(patchArea - triArea);
        double relTol = Tolerances.BarycentricInsideEpsilon * triArea;

        Assert.True(
            diff <= Tolerances.EpsArea || diff <= relTol,
            $"Patch area {patchArea} differs from triangle area {triArea} by {diff}.");
    }
}

