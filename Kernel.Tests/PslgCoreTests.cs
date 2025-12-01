using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Geometry.Predicates;
using Kernel;
using Xunit;

namespace Kernel.Tests;

public class PslgCoreTests
{
    private static Triangle MakeCanonicalTriangle()
        => Triangle.FromWinding(
            new Point(0, 0, 0),
            new Point(1, 0, 0),
            new Point(0, 1, 0));

    private static PslgResult RunPslg(
        Triangle triangle,
        IReadOnlyList<TriangleSubdivision.IntersectionPoint> points,
        IReadOnlyList<TriangleSubdivision.IntersectionSegment> segments)
    {
        var input = new PslgInput(in triangle, points, segments);
        return PslgBuilder.Run(in input);
    }

    [Fact]
    public void Build_NoSegments_AddsTriangleCornersAndBoundaryEdges()
    {
        var points = new List<TriangleSubdivision.IntersectionPoint>();
        var segments = new List<TriangleSubdivision.IntersectionSegment>();

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
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(new Barycentric(0.6, 0.3, 0.1), new RealPoint(0, 0, 0)),
            new TriangleSubdivision.IntersectionPoint(new Barycentric(0.2, 0.5, 0.3), new RealPoint(0, 0, 0))
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            new TriangleSubdivision.IntersectionSegment(0, 1)
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

        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.2, 0.3, 0.5),
                new RealPoint(0, 0, 0)),
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.2 + delta, 0.3 - delta, 0.5),
                new RealPoint(0, 0, 0))
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>();

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

        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            // Near V0 = (u,v) = (1,0).
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(1.0 - delta, delta, 0.0),
                new RealPoint(0, 0, 0)),

            // Near V1 = (u,v) = (0,1).
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.0, 1.0 - delta, delta),
                new RealPoint(0, 0, 0)),

            // Near V2 = (u,v) = (0,0).
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(delta, 0.0, 1.0 - delta),
                new RealPoint(0, 0, 0))
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>();

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
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            // Segment A: P0 -> P1, roughly horizontal.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.2, 0.2, 0.6),
                new RealPoint(0, 0, 0)), // P0: (u,v) = (0.2, 0.2)
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.8, 0.2, 0.0),
                new RealPoint(0, 0, 0)), // P1: (0.8, 0.2)

            // Segment B: P2 -> P3, roughly vertical.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.5, 0.0, 0.5),
                new RealPoint(0, 0, 0)), // P2: (0.5, 0.0)
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.5, 0.5, 0.0),
                new RealPoint(0, 0, 0))  // P3: (0.5, 0.5)
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            // Crossing segments: (P0,P1) and (P2,P3).
            new TriangleSubdivision.IntersectionSegment(0, 1),
            new TriangleSubdivision.IntersectionSegment(2, 3)
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
    public void PslgEdges_CrossingWithExplicitVertex_IsAccepted()
    {
        // Same geometric configuration as PslgEdges_NoCrossingsAllowed, but
        // we add an explicit vertex at the crossing and split segments so
        // that all edges meet at that vertex.
        var intersection = new Barycentric(0.5, 0.2, 0.3); // (u,v) = (0.5, 0.2)

        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            // P0 and P1 as before on the nearly horizontal segment.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.2, 0.2, 0.6),
                new RealPoint(0, 0, 0)), // index 0
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.8, 0.2, 0.0),
                new RealPoint(0, 0, 0)), // index 1

            // P2 and P3 on the nearly vertical segment.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.5, 0.0, 0.5),
                new RealPoint(0, 0, 0)), // index 2
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.5, 0.5, 0.0),
                new RealPoint(0, 0, 0)), // index 3

            // Explicit crossing point.
            new TriangleSubdivision.IntersectionPoint(
                intersection,
                new RealPoint(0, 0, 0)) // index 4
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            // Split segment A: P0 -> I, I -> P1.
            new TriangleSubdivision.IntersectionSegment(0, 4),
            new TriangleSubdivision.IntersectionSegment(4, 1),

            // Split segment B: P2 -> I, I -> P3.
            new TriangleSubdivision.IntersectionSegment(2, 4),
            new TriangleSubdivision.IntersectionSegment(4, 3)
        };

        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);

        Assert.NotEmpty(result.Vertices);
        Assert.NotEmpty(result.Edges);
    }

    [Fact]
    public void PslgBoundaryEdges_SplitAndOrdered()
    {
        // Points on each boundary side to ensure splitting and ordering.
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            // Side V0-V1 (w = 0): v = 0.25 and v = 0.75.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.75, 0.25, 0.0),
                new RealPoint(0, 0, 0)), // index 0 -> vertex 3
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.25, 0.75, 0.0),
                new RealPoint(0, 0, 0)), // index 1 -> vertex 4

            // Side V1-V2 (u = 0): v = 0.25 and v = 0.6.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.0, 0.25, 0.75),
                new RealPoint(0, 0, 0)), // index 2 -> vertex 5
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.0, 0.6, 0.4),
                new RealPoint(0, 0, 0)), // index 3 -> vertex 6

            // Side V2-V0 (v = 0): u = 0.3 and u = 0.6.
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.3, 0.0, 0.7),
                new RealPoint(0, 0, 0)), // index 4 -> vertex 7
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.6, 0.0, 0.4),
                new RealPoint(0, 0, 0))  // index 5 -> vertex 8
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>();

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
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(1.0 - delta, delta, 0.0),
                new RealPoint(0, 0, 0)), // near V0
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(1.0 - 2 * delta, 2 * delta, 0.0),
                new RealPoint(0, 0, 0)), // also near V0
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.4, 0.3, 0.3),
                new RealPoint(0, 0, 0))   // interior
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            // Degenerate after snapping: both endpoints map to V0.
            new TriangleSubdivision.IntersectionSegment(0, 1),
            // Valid segment V0 -> interior (in both orders, should dedup).
            new TriangleSubdivision.IntersectionSegment(0, 2),
            new TriangleSubdivision.IntersectionSegment(2, 0)
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
        var points = new List<TriangleSubdivision.IntersectionPoint>();
        var segments = new List<TriangleSubdivision.IntersectionSegment>();

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
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.6, 0.4, 0.0),
                new RealPoint(0, 0, 0)),
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.0, 0.6, 0.4),
                new RealPoint(0, 0, 0))
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            new TriangleSubdivision.IntersectionSegment(0, 1)
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
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.6, 0.4, 0.0),
                new RealPoint(0, 0, 0)),
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.0, 0.6, 0.4),
                new RealPoint(0, 0, 0))
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            new TriangleSubdivision.IntersectionSegment(0, 1)
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
        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.6, 0.4, 0.0),
                new RealPoint(0, 0, 0)),
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.0, 0.6, 0.4),
                new RealPoint(0, 0, 0))
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            new TriangleSubdivision.IntersectionSegment(0, 1)
        };

        var triangle = MakeCanonicalTriangle();
        var result = RunPslg(triangle, points, segments);

        Assert.Equal(2, result.Selection.InteriorFaces.Count);
        Assert.InRange(result.Selection.InteriorFaces.Sum(f => Math.Abs(f.SignedAreaUV)), 0.5 - 1e-6, 0.5 + 1e-6);
    }

    [Fact]
    public void EarClipping_ConvexQuad()
    {
        var vertices = new List<PslgVertex>
        {
            new PslgVertex(0, 0, false, -1),
            new PslgVertex(1, 0, false, -1),
            new PslgVertex(1, 1, false, -1),
            new PslgVertex(0, 1, false, -1)
        };

        double faceArea = new RealPolygon(new[]
        {
            new RealPoint(vertices[0].X, vertices[0].Y, 0.0),
            new RealPoint(vertices[1].X, vertices[1].Y, 0.0),
            new RealPoint(vertices[2].X, vertices[2].Y, 0.0),
            new RealPoint(vertices[3].X, vertices[3].Y, 0.0)
        }).SignedArea;
        var face = new PslgFace(new[] { 0, 1, 2, 3 }, signedArea: faceArea);

        var tris = PslgBuilder.TriangulateSimple(face.OuterVertices, vertices, faceArea);
        Assert.Equal(2, tris.Count);

        double areaSum = 0.0;
        foreach (var t in tris)
        {
            areaSum += Math.Abs(new RealTriangle(
                new RealPoint(vertices[t.A].X, vertices[t.A].Y, 0.0),
                new RealPoint(vertices[t.B].X, vertices[t.B].Y, 0.0),
                new RealPoint(vertices[t.C].X, vertices[t.C].Y, 0.0)).SignedArea);
        }

        Assert.InRange(areaSum, 1.0 - 1e-9, 1.0 + 1e-9);
    }

    [Fact]
    public void EarClipping_ConcavePolygon()
    {
        var vertices = new List<PslgVertex>
        {
            new PslgVertex(0, 0, false, -1),
            new PslgVertex(2, 0, false, -1),
            new PslgVertex(2, 1, false, -1),
            new PslgVertex(1, 0.2, false, -1), // dent
            new PslgVertex(0, 1, false, -1)
        };

        double faceArea = new RealPolygon(new[]
        {
            new RealPoint(vertices[0].X, vertices[0].Y, 0.0),
            new RealPoint(vertices[1].X, vertices[1].Y, 0.0),
            new RealPoint(vertices[2].X, vertices[2].Y, 0.0),
            new RealPoint(vertices[3].X, vertices[3].Y, 0.0),
            new RealPoint(vertices[4].X, vertices[4].Y, 0.0)
        }).SignedArea;
        var face = new PslgFace(new[] { 0, 1, 2, 3, 4 }, signedArea: faceArea);

        var tris = PslgBuilder.TriangulateSimple(face.OuterVertices, vertices, faceArea);
        Assert.Equal(3, tris.Count);

        double areaSum = 0.0;
        foreach (var t in tris)
        {
            areaSum += Math.Abs(new RealTriangle(
                new RealPoint(vertices[t.A].X, vertices[t.A].Y, 0.0),
                new RealPoint(vertices[t.B].X, vertices[t.B].Y, 0.0),
                new RealPoint(vertices[t.C].X, vertices[t.C].Y, 0.0)).SignedArea);
        }

        Assert.InRange(areaSum, Math.Abs(faceArea) - 1e-9, Math.Abs(faceArea) + 1e-9);
    }

    [Fact]
    public void EarClipping_ThrowsOnSelfIntersectingPolygon()
    {
        // Bow-tie ordering (self-intersecting).
        var vertices = new List<PslgVertex>
        {
            new PslgVertex(0, 0, false, -1),
            new PslgVertex(1, 1, false, -1),
            new PslgVertex(0, 1, false, -1),
            new PslgVertex(1, 0, false, -1)
        };

        var face = new PslgFace(new[] { 0, 1, 2, 3 }, signedArea: 0.0);

        Assert.Throws<InvalidOperationException>(
            () => PslgBuilder.TriangulateSimple(face.OuterVertices, vertices, expectedArea: 0.0));
    }

    [Fact]
    public void TriangulateInteriorFaces_MultipleChords_CoversSegmentsAndArea()
    {
        var v0 = new Point(0, 0, 0);
        var v1 = new Point(1, 0, 0);
        var v2 = new Point(0, 1, 0);
        var tri = new Triangle(v0, v1, v2, new Point(0, 0, 1));

        var points = new List<TriangleSubdivision.IntersectionPoint>
        {
            new TriangleSubdivision.IntersectionPoint(new Barycentric(0.8, 0.2, 0.0), new RealPoint(0,0,0)), // P0 on V0-V1
            new TriangleSubdivision.IntersectionPoint(new Barycentric(0.0, 0.7, 0.3), new RealPoint(0,0,0)), // P1 on V1-V2
            new TriangleSubdivision.IntersectionPoint(new Barycentric(0.3, 0.0, 0.7), new RealPoint(0,0,0)), // P2 on V0-V2
            new TriangleSubdivision.IntersectionPoint(new Barycentric(0.0, 0.3, 0.7), new RealPoint(0,0,0))  // P3 on V1-V2
        };

        var segments = new List<TriangleSubdivision.IntersectionSegment>
        {
            new TriangleSubdivision.IntersectionSegment(0, 1),
            new TriangleSubdivision.IntersectionSegment(2, 3)
        };

        var result = RunPslg(tri, points, segments);

        // Triangulate and map to patches.
        var patches = result.Patches;

        // Area check.
        double patchArea = patches.Sum(p => Math.Abs(new RealTriangle(p.P0, p.P1, p.P2).SignedArea));
        Assert.InRange(patchArea, 0.5 - 1e-6, 0.5 + 1e-6);

        // Every non-boundary PSLG edge should appear as a patch edge.
        var triangleEdges = new HashSet<(int, int)>();

        // Map PSLG vertices back to world-space points using the same
        // barycentric mapping as the PSLG pipeline.
        var vertexWorld = new List<RealPoint>(result.Vertices.Count);
        for (int i = 0; i < result.Vertices.Count; i++)
        {
            var v = result.Vertices[i];
            double u = v.X;
            double vCoord = v.Y;
            double w = 1.0 - u - vCoord;
            var barycentric = new Barycentric(u, vCoord, w);
            vertexWorld.Add(Barycentric.ToRealPointOnTriangle(in tri, in barycentric));
        }

        // For each patch triangle, recover the underlying PSLG vertex indices
        // by nearest-neighbour search in world space, then record its edges.
        for (int i = 0; i < patches.Count; i++)
        {
            var patch = patches[i];
            int ia = FindNearestVertexIndex(patch.P0, vertexWorld);
            int ib = FindNearestVertexIndex(patch.P1, vertexWorld);
            int ic = FindNearestVertexIndex(patch.P2, vertexWorld);

            AddEdge(triangleEdges, ia, ib);
            AddEdge(triangleEdges, ib, ic);
            AddEdge(triangleEdges, ic, ia);
        }

        foreach (var e in result.Edges.Where(e => !e.IsBoundary))
        {
            Assert.Contains(NormalizeEdge(e.Start, e.End), triangleEdges);
        }
    }

    private static int FindNearestVertexIndex(RealPoint point, IReadOnlyList<RealPoint> vertices)
    {
        if (vertices is null || vertices.Count == 0)
            throw new ArgumentException("Vertex list must be non-empty.", nameof(vertices));

        int bestIndex = 0;
        double bestDistSq = point.DistanceSquared(vertices[0]);

        for (int i = 1; i < vertices.Count; i++)
        {
            double distSq = point.DistanceSquared(vertices[i]);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static (int, int) NormalizeEdge(int a, int b) => a < b ? (a, b) : (b, a);

    private static void AddEdge(HashSet<(int, int)> set, int a, int b) => set.Add(NormalizeEdge(a, b));
}
