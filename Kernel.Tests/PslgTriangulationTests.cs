using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Kernel;
using Xunit;

namespace Kernel.Tests;

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

        var onEdgePoints = new List<TriangleSubdivision.IntersectionPoint>
        {
            MakeOnEdgePoint(triangle, 0.7233333333333334),
            MakeOnEdgePoint(triangle, 0.8333333333333334),
            MakeOnEdgePoint(triangle, 0.16666666666666669),
        };

        // Force the PSLG path (skip the trivial "no segments" early return)
        // by adding a single segment along that same edge.
        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            new(startIndex: 0, endIndex: 2)
        };

        PslgBuilder.Build(onEdgePoints, segments, out var vertices, out var edges);
        PslgBuilder.BuildHalfEdges(vertices, edges, out var halfEdges);
        var faces = PslgBuilder.ExtractFaces(vertices, halfEdges);
        var interior = PslgBuilder.SelectInteriorFaces(faces);

        Assert.NotEmpty(interior);

        var largest = interior.OrderByDescending(f => Math.Abs(f.SignedAreaUV)).First();

        Assert.Contains(0, largest.OuterVertices); // V0
        Assert.Contains(1, largest.OuterVertices); // V1
        Assert.Contains(2, largest.OuterVertices); // V2

        var outer = largest.OuterVertices
            .Select(i => new RealPoint(vertices[i].X, vertices[i].Y, 0.0))
            .ToList();
        double polygonArea = new RealPolygon(outer).SignedArea;

        Assert.True(
            Math.Abs(polygonArea - TriangleSubdivision.ReferenceTriangleAreaUv) <= 1e-6,
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

        // This should triangulate into four positive-area triangles
        // (0,1,4), (0,4,3), (0,3,5), (0,5,2) despite the collinear chain.
        var triangles = PslgBuilder.TriangulateSimple(polygon, vertices, expectedArea);

        Assert.Equal(polygon.Length - 2, triangles.Count);
    }

    private static TriangleSubdivision.IntersectionPoint MakeOnEdgePoint(
        Triangle triangle,
        double vOnEdge)
    {
        var bary = new Barycentric(0.0, vOnEdge, 1.0 - vOnEdge);
        var pos = triangle.FromBarycentric(bary);
        return new TriangleSubdivision.IntersectionPoint(bary, pos);
    }
}
