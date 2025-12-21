using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Boolean;
using Pslg;
using Xunit;

namespace Boolean.Tests;

public class PslgTriangulationTests
{
    [Fact]
    public void ExtractFaces_ShouldKeepAllCorners_WhenEdgeHasOnEdgeVertices()
    {
        // Captured on-edge barycentric coords from the boolean failure dump:
        // all lie on the V1-V2 edge (u = 0), splitting that side multiple times.
        var triangle = Triangle.FromWinding(
            new Point(0, 0, 0),
            new Point(1, 0, 0),
            new Point(0, 1, 0));

        var onEdgePoints = new List<Triangulation.IntersectionPoint>
        {
            MakeOnEdgePoint(triangle, 0.7233333333333334),
            MakeOnEdgePoint(triangle, 0.8333333333333334),
            MakeOnEdgePoint(triangle, 0.16666666666666669),
        };

        // Force the PSLG path (skip the trivial "no segments" early return)
        // by adding a single segment along that same edge.
        var segments = new List<Triangulation.IntersectionSegment>
        {
            new(startIndex: 0, endIndex: 2)
        };

        var pslgPoints = new List<PslgPoint>(onEdgePoints.Count);
        for (int i = 0; i < onEdgePoints.Count; i++)
        {
            pslgPoints.Add(new PslgPoint(onEdgePoints[i].Barycentric));
        }

        var pslgSegments = new List<PslgSegment>(segments.Count);
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            pslgSegments.Add(new PslgSegment(seg.StartIndex, seg.EndIndex));
        }

        var input = new PslgInput(in triangle, pslgPoints, pslgSegments);
        var result = PslgBuilder.Run(in input);

        Assert.NotEmpty(result.Selection.InteriorFaces);

        var largest = result.Selection.InteriorFaces
            .OrderByDescending(f => Math.Abs(f.SignedAreaUV))
            .First();

        Assert.Contains(0, largest.OuterVertices); // V0
        Assert.Contains(1, largest.OuterVertices); // V1
        Assert.Contains(2, largest.OuterVertices); // V2

        var outer = largest.OuterVertices
            .Select(i => new RealPoint(result.Vertices[i].X, result.Vertices[i].Y, 0.0))
            .ToList();
        double polygonArea = new RealPolygon(outer).SignedArea;

        Assert.True(
            Math.Abs(polygonArea - Triangulation.ReferenceTriangleAreaUv) <= 1e-6,
            $"Interior face lost area: outer=[{string.Join(",", largest.OuterVertices)}], area={polygonArea}");
    }

    [Fact]
    public void TriangulateSimple_ShouldHandleCollinearEdgeSubdivisions()
    {
        // Full outer ring from the dump (before ear removal dropped corner 0).
        var vertices = new List<PslgVertex>
    {
        new(1, 0, true, 0),                     // 0
        new(0, 1, true, 1),                     // 1
        new(0, 0, true, 2),                     // 2
        new(0, 0.7233333333333334, false, -1),  // 3
        new(0, 0.8333333333333334, false, -1),  // 4
        new(0, 0.16666666666666669, false, -1), // 5
    };

        int[] polygon = { 0, 1, 4, 3, 5, 2 };
        double expectedArea = 0.5;

        // Act – must not throw even though the left edge is subdivided
        // by collinear intermediate points.
        var triangles = TriangulationTriangulator.TriangulateSimple(polygon, vertices, expectedArea);

        Assert.NotEmpty(triangles);

        // Check that total area of produced triangles matches the expected polygon area.
        double totalArea = 0.0;
        foreach (var (a, b, c) in triangles)
        {
            var t = new RealTriangle(
                new RealPoint(vertices[a].X, vertices[a].Y, 0.0),
                new RealPoint(vertices[b].X, vertices[b].Y, 0.0),
                new RealPoint(vertices[c].X, vertices[c].Y, 0.0));
            totalArea += Math.Abs(t.SignedArea);
        }

        double diff = Math.Abs(totalArea - expectedArea);
        double relTol = Tolerances.BarycentricInsideEpsilon * expectedArea;

        Assert.True(
            diff <= Tolerances.EpsArea || diff <= relTol,
            $"Total triangle area {totalArea} differs from expected {expectedArea} by {diff}.");
    }


    [Fact]
    public void RectWithEdgeSubdivisions_ShouldTriangulateAndPreserveArea()
    {
        // Outer rectangle: (-1,-1) -> (1,-1) -> (1,1) -> (-1,1)
        // Add two extra points on bottom edge: (-0.3,-1), (0.4,-1)
        var vertices = new[]
        {
            new PslgVertex(-1, -1, false, -1),
            new PslgVertex(-0.3, -1, false, -1),
            new PslgVertex(0.4, -1, false, -1),
            new PslgVertex(1, -1, false, -1),
            new PslgVertex(1, 1, false, -1),
            new PslgVertex(-1, 1, false, -1),
        };

        int[] polygon = { 0, 1, 2, 3, 4, 5 };
        double expectedArea = 4.0; // 2x2 rectangle

        var tris = TriangulationTriangulator.TriangulateSimple(polygon, vertices, expectedArea);

        Assert.NotEmpty(tris);

        double sum = 0;
        foreach (var (a, b, c) in tris)
        {
            sum += TriangleArea(vertices[a], vertices[b], vertices[c]);
        }

        Assert.InRange(sum, expectedArea - 1e-8, expectedArea + 1e-8);
    }

    private static double TriangleArea(PslgVertex a, PslgVertex b, PslgVertex c)
    {
        var ax = b.X - a.X;
        var ay = b.Y - a.Y;
        var bx = c.X - a.X;
        var by = c.Y - a.Y;
        return 0.5 * Math.Abs(ax * by - ay * bx);
    }

    private static Triangulation.IntersectionPoint MakeOnEdgePoint(
        Triangle triangle,
        double vOnEdge)
    {
        var bary = new Barycentric(0.0, vOnEdge, 1.0 - vOnEdge);
        var pos = Barycentric.ToRealPointOnTriangle(in triangle, in bary);
        return new Triangulation.IntersectionPoint(bary, pos);
    }
}
