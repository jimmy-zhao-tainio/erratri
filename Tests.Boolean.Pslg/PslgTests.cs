using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Pslg;
using Xunit;

namespace Tests.Boolean.Pslg;

public class PslgTests
{
    private static Triangle MakeCanonicalTriangle()
        => Triangle.FromWinding(
            new Point(0, 0, 0),
            new Point(1, 0, 0),
            new Point(0, 1, 0));
    private static PslgOutput RunPslg(
        Triangle triangle,
        IReadOnlyList<PslgPoint> points,
        IReadOnlyList<PslgSegment> segments)
    {
        var input = new PslgInput(in triangle, points, segments);
        return PslgBuilder.Run(in input);
    }

    [Fact]
    public void Build_NoSegments_AddsTriangleCornersAndBoundaryEdges()
    {
        var points = new List<PslgPoint>();
        var segments = new List<PslgSegment>();
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var vertices = result.Vertices;
        var edges = result.Edges;
        Assert.Equal(3, vertices.Count);
        Assert.Equal(3, edges.Count);
        // Corner vertices in barycentric (u,v) chart.
        Assert.True(vertices[0].IsTriangleCorner);
        Assert.Equal(0, vertices[0].CornerIndex);
        Assert.Equal(1.0, vertices[0].X, 10);
        Assert.Equal(0.0, vertices[0].Y, 10);
        Assert.True(vertices[1].IsTriangleCorner);
        Assert.Equal(1, vertices[1].CornerIndex);
        Assert.Equal(0.0, vertices[1].X, 10);
        Assert.Equal(1.0, vertices[1].Y, 10);
        Assert.True(vertices[2].IsTriangleCorner);
        Assert.Equal(2, vertices[2].CornerIndex);
        Assert.Equal(0.0, vertices[2].X, 10);
        Assert.Equal(0.0, vertices[2].Y, 10);
        // Boundary edges form the cycle 0->1->2->0.
        Assert.True(edges[0].IsBoundary);
        Assert.True(edges[1].IsBoundary);
        Assert.True(edges[2].IsBoundary);
        Assert.Equal(0, edges[0].Start);
        Assert.Equal(1, edges[0].End);
        Assert.Equal(1, edges[1].Start);
        Assert.Equal(2, edges[1].End);
        Assert.Equal(2, edges[2].Start);
        Assert.Equal(0, edges[2].End);
    }

    [Fact]
    public void Build_SingleSegment_AddsIntersectionVerticesAndEdge()
    {
        // Two intersection points with arbitrary barycentrics.
        var points = new List<PslgPoint>
        {
            new PslgPoint(new Barycentric(0.6, 0.3, 0.1)),
            new PslgPoint(new Barycentric(0.2, 0.5, 0.3))
        };
        var segments = new List<PslgSegment>
        {
            new PslgSegment(0, 1)
        };
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var vertices = result.Vertices;
        var edges = result.Edges;
        // 3 triangle corners + 2 intersection poinTriangleSubdivision.
        Assert.Equal(5, vertices.Count);
        // 3 boundary edges + 1 segment edge.
        Assert.Equal(4, edges.Count);
        // Intersection vertices use (u,v) coordinates.
        Assert.False(vertices[3].IsTriangleCorner);
        Assert.Equal(-1, vertices[3].CornerIndex);
        Assert.Equal(0.6, vertices[3].X, 10);
        Assert.Equal(0.3, vertices[3].Y, 10);
        Assert.False(vertices[4].IsTriangleCorner);
        Assert.Equal(-1, vertices[4].CornerIndex);
        Assert.Equal(0.2, vertices[4].X, 10);
        Assert.Equal(0.5, vertices[4].Y, 10);
        // Segment edge connects the two intersection vertices.
        var segEdge = edges[3];
        Assert.False(segEdge.IsBoundary);
        Assert.Equal(3, segEdge.Start);
        Assert.Equal(4, segEdge.End);
    }

    [Fact]
    public void PslgVertexSnapping_MergesNearDuplicates()
    {
        double delta = 1e-8;
        var points = new List<PslgPoint>
        {
            new PslgPoint(new Barycentric(0.2, 0.3, 0.5)),
            new PslgPoint(new Barycentric(0.2 + delta, 0.3 - delta, 0.5))
        };
        var segments = new List<PslgSegment>();
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var vertices = result.Vertices;
        // 3 triangle corners + 1 merged intersection vertex.
        Assert.Equal(4, vertices.Count);
        var merged = vertices[3];
        Assert.False(merged.IsTriangleCorner);
        Assert.Equal(0.2, merged.X, 6);
        Assert.Equal(0.3, merged.Y, 6);
    }

    [Fact]
    public void PslgVertexSnapping_SnapsCorners()
    {
        double delta = 5e-8;
        var points = new List<PslgPoint>
        {
            // Near V0 = (u,v) = (1,0).
            new PslgPoint(new Barycentric(1.0 - delta, delta, 0.0)),
            // Near V1 = (u,v) = (0,1).
            new PslgPoint(new Barycentric(0.0, 1.0 - delta, delta)),
            // Near V2 = (u,v) = (0,0).
            new PslgPoint(new Barycentric(delta, 0.0, 1.0 - delta))
        };
        var segments = new List<PslgSegment>();
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var vertices = result.Vertices;
        // All near-corner vertices snap into the existing triangle-corner vertices.
        Assert.Equal(3, vertices.Count);
        Assert.True(vertices[0].IsTriangleCorner);
        Assert.Equal(0, vertices[0].CornerIndex);
        Assert.Equal(1.0, vertices[0].X, 10);
        Assert.Equal(0.0, vertices[0].Y, 10);
        Assert.True(vertices[1].IsTriangleCorner);
        Assert.Equal(1, vertices[1].CornerIndex);
        Assert.Equal(0.0, vertices[1].X, 10);
        Assert.Equal(1.0, vertices[1].Y, 10);
        Assert.True(vertices[2].IsTriangleCorner);
        Assert.Equal(2, vertices[2].CornerIndex);
        Assert.Equal(0.0, vertices[2].X, 10);
        Assert.Equal(0.0, vertices[2].Y, 10);
    }

    [Fact]
    public void PslgEdges_NoCrossingsAllowed()
    {
        // Construct an "X" crossing in param space with no explicit vertex
        // at the crossing. This must be rejected by the PSLG consistency
        // checks (Phase D3).
        var points = new List<PslgPoint>
        {
            // Segment A: P0 -> P1, roughly horizontal.
            new PslgPoint(new Barycentric(0.2, 0.2, 0.6)), // P0: (u,v) = (0.2, 0.2)
            new PslgPoint(new Barycentric(0.8, 0.2, 0.0)), // P1: (0.8, 0.2)
            // Segment B: P2 -> P3, roughly vertical.
            new PslgPoint(new Barycentric(0.5, 0.0, 0.5)), // P2: (0.5, 0.0)
            new PslgPoint(new Barycentric(0.5, 0.5, 0.0))  // P3: (0.5, 0.5)
        };
        var segments = new List<PslgSegment>
        {
            // Crossing segments: (P0,P1) and (P2,P3).
            new PslgSegment(0, 1),
            new PslgSegment(2, 3)
        };
        var ex = Assert.Throws<InvalidOperationException>(
            () =>
            {
                var triangle = MakeCanonicalTriangle();
                RunPslg(triangle, points, segments);
            });
        Assert.Contains("PSLG requires no crossings without vertices", ex.Message);
    }

    [Fact]
    public void PslgBoundaryEdges_SplitAndOrdered()
    {
        // Points on each boundary side to ensure splitting and ordering.
        var points = new List<PslgPoint>
        {
            // Side V0-V1 (w = 0): v = 0.25 and v = 0.75.
            new PslgPoint(new Barycentric(0.75, 0.25, 0.0)), // index 0 -> vertex 3
            new PslgPoint(new Barycentric(0.25, 0.75, 0.0)), // index 1 -> vertex 4
            // Side V1-V2 (u = 0): v = 0.25 and v = 0.6.
            new PslgPoint(new Barycentric(0.0, 0.25, 0.75)), // index 2 -> vertex 5
            new PslgPoint(new Barycentric(0.0, 0.6, 0.4)), // index 3 -> vertex 6
            // Side V2-V0 (v = 0): u = 0.3 and u = 0.6.
            new PslgPoint(new Barycentric(0.3, 0.0, 0.7)), // index 4 -> vertex 7
            new PslgPoint(new Barycentric(0.6, 0.0, 0.4))  // index 5 -> vertex 8
        };
        var segments = new List<PslgSegment>();
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        // 3 corners + 6 boundary points (no dedup).
        Assert.Equal(9, result.Vertices.Count);
        // Expect 9 boundary edges (split along each side), all marked boundary.
        Assert.Equal(9, result.Edges.Count);
        Assert.All(result.Edges, e => Assert.True(e.IsBoundary));
        var edgeSet = new HashSet<(int, int)>();
        foreach (var e in result.Edges)
        {
            edgeSet.Add((e.Start, e.End));
        }
        var expected = new HashSet<(int, int)>
        {
            // Side V0->V1: 0 -> 3 -> 4 -> 1
            (0, 3), (3, 4), (4, 1),
            // Side V1->V2: 1 -> 6 -> 5 -> 2  (reverse ordering on this side)
            (1, 6), (6, 5), (5, 2),
            // Side V2->V0: 2 -> 7 -> 8 -> 0
            (2, 7), (7, 8), (8, 0)
        };
        Assert.Equal(expected, edgeSet);
    }

    [Fact]
    public void PslgIntersectionEdges_DedupesAndSkipsDegenerateSegments()
    {
        double delta = 1e-8;
        // Two points near V0 that will snap/dedup to the corner,
        // plus one interior point.
        var points = new List<PslgPoint>
        {
            new PslgPoint(new Barycentric(1.0 - delta, delta, 0.0)), // near V0
            new PslgPoint(new Barycentric(1.0 - 2 * delta, 2 * delta, 0.0)), // also near V0
            new PslgPoint(new Barycentric(0.4, 0.3, 0.3))   // interior
        };
        var segments = new List<PslgSegment>
        {
            // Degenerate after snapping: both endpoints map to V0.
            new PslgSegment(0, 1),
            // Valid segment V0 -> interior (in both orders, should dedup).
            new PslgSegment(0, 2),
            new PslgSegment(2, 0)
        };
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var edges = result.Edges;
        // Count boundary vs intersection edges.
        int boundaryCount = 0;
        int interiorCount = 0;
        var interiorEdges = new List<PslgEdge>();
        foreach (var e in edges)
        {
            if (e.IsBoundary)
            {
                boundaryCount++;
            }
            else
            {
                interiorCount++;
                interiorEdges.Add(e);
            }
        }
        Assert.Equal(3, boundaryCount);   // corners only on boundary.
        Assert.Equal(1, interiorCount);   // deduped to a single segment.
        var seg = Assert.Single(interiorEdges);
        int a = Math.Min(seg.Start, seg.End);
        int b = Math.Max(seg.Start, seg.End);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void HalfEdge_FaceExtraction_SimpleTriangle()
    {
        var points = new List<PslgPoint>();
        var segments = new List<PslgSegment>();
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var faces = result.Faces;
        Assert.Single(faces);
        double area = Math.Abs(faces[0].SignedAreaUV);
        Assert.InRange(area, 0.5 - 1e-6, 0.5 + 1e-6);
    }

    [Fact]
    public void HalfEdge_FaceExtraction_SingleChord()
    {
        // One chord splitting the triangle into two interior faces.
        var points = new List<PslgPoint>
        {
            new PslgPoint(new Barycentric(0.6, 0.4, 0.0)),
            new PslgPoint(new Barycentric(0.0, 0.6, 0.4))
        };
        var segments = new List<PslgSegment>
        {
            new PslgSegment(0, 1)
        };
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        Assert.Equal(2, result.Faces.Count); // two bounded regions
        double areaSum = result.Faces.Sum(f => Math.Abs(f.SignedAreaUV));
        Assert.InRange(areaSum, 0.5 - 1e-6, 0.5 + 1e-6);
    }

    [Fact]
    public void SelectInteriorFaces_RemovesOuterAndKeepsArea()
    {
        // Triangle with a chord: same setup as the previous test but exercise
        // the interior-face selector.
        var points = new List<PslgPoint>
        {
            new PslgPoint(new Barycentric(0.6, 0.4, 0.0)),
            new PslgPoint(new Barycentric(0.0, 0.6, 0.4))
        };
        var segments = new List<PslgSegment>
        {
            new PslgSegment(0, 1)
        };
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        var interiors = result.Selection.InteriorFaces;
        Assert.Equal(2, interiors.Count);
        double areaSum = interiors.Sum(f => Math.Abs(f.SignedAreaUV));
        Assert.InRange(areaSum, 0.5 - 1e-6, 0.5 + 1e-6);
    }

    [Fact]
    public void SelectInteriorFaces_WithExpectedArea_PassesForValidPslg()
    {
        var points = new List<PslgPoint>
        {
            new PslgPoint(new Barycentric(0.6, 0.4, 0.0)),
            new PslgPoint(new Barycentric(0.0, 0.6, 0.4))
        };
        var segments = new List<PslgSegment>
        {
            new PslgSegment(0, 1)
        };
        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);
        Assert.Equal(2, result.Selection.InteriorFaces.Count);
        Assert.InRange(result.Selection.InteriorFaces.Sum(f => Math.Abs(f.SignedAreaUV)), 0.5 - 1e-6, 0.5 + 1e-6);
    }
}

