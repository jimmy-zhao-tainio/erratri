using Geometry;
using Geometry.Predicates;
using Geometry.Topology;
using Xunit;

namespace Tests.Geometry.Topology;

public class IntersectionTypesTests
{
    // ----------------------------------------------------------------------
    // Coplanar: no intersection (None)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarDisjointTriangles_ReturnsNone()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(4, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarDisjointTriangles_Shifted_ReturnsNone()
    {
        // Two congruent triangles in the plane z = 0, translated apart.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 1, 0);
        var a2 = new Point(0, 3, 0);

        var b0 = new Point(5, 0, 0);
        var b1 = new Point(7, 1, 0);
        var b2 = new Point(5, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarBoundingBoxesOverlapButDisjoint_ReturnsNone()
    {
        // Triangles lie in the same plane; their axis-aligned bounding boxes overlap,
        // but the triangles themselves are disjoint.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(4, 4, 0);
        var b1 = new Point(2, 4, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarCollinearEdgesDisjoint_ReturnsNone()
    {
        // Two triangles whose bottom edges lie on the same line (y = 0)
        // but the segments are disjoint, so there is no contact.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(3, 0, 0);
        var a2 = new Point(0, 3, 0);

        var b0 = new Point(5, 0, 0);
        var b1 = new Point(8, 0, 0);
        var b2 = new Point(5, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Coplanar: area intersections (Area)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarIdenticalTriangles_ReturnsArea()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(a0, a1, a2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarOverlappingTriangles_ReturnsArea()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarTriangleInsideTriangle_NoSharedEdges_ReturnsArea()
    {
        // Small triangle completely inside a larger one, with no shared edges.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(8, 0, 0);
        var a2 = new Point(0, 8, 0);

        var b0 = new Point(3, 3, 0);
        var b1 = new Point(4, 3, 0);
        var b2 = new Point(3, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarTriangleInsideTriangle_SharedEdge_ReturnsArea()
    {
        // Small triangle completely inside a larger one, sharing one full edge.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(3, 0, 0);
        var b2 = new Point(0, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarSharedEdgeFullOverlap_ReturnsSegment()
    {
        // Two coplanar triangles sharing one full edge, no area overlap.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(5, 0, 0);
        var a2 = new Point(0, 5, 0);

        var b0 = new Point(5, 5, 0);
        var b1 = new Point(5, 0, 0);
        var b2 = new Point(0, 5, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarSharedVertexAndAreaOverlap_ReturnsArea()
    {
        // Triangles share the origin as a vertex and overlap in area near that corner.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(3, 1, 0);
        var b2 = new Point(1, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Coplanar: segment / edge intersections (Segment)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarSharedEdge_ReturnsSegment()
    {
        // Two coplanar triangles sharing the edge from (0,0,0) to (4,0,0).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, -4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarPartiallyOverlappingEdges_ReturnsSegment()
    {
        // Edge of A from (0,0,0) to (4,0,0); edge of B from (2,0,0) to (6,0,0).
        // Overlap is the segment [2,4] on the x-axis; triangles lie on opposite sides.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(2, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(6, -2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarLongerEdgeContainsShorterEdge_ReturnsSegment()
    {
        // Edge of B lies entirely inside the edge of A; triangles are on opposite sides.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 3, 0);

        var b0 = new Point(2, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, -3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Coplanar: point intersections (Point)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarSharedVertexOnly_ReturnsPoint()
    {
        // Two triangles share only the origin as a common vertex.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(-4, 0, 0);
        var b2 = new Point(0, -4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarVertexOfAOnEdgeInteriorOfB_ReturnsPoint()
    {
        // Vertex of A lies on the interior of an edge of B, with no area overlap.
        var b0 = new Point(0, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(0, 4, 0);

        var a0 = new Point(1, 3, 0); // lies on the edge from (4,0,0) to (0,4,0)
        var a1 = new Point(3, 5, 0);
        var a2 = new Point(1, 5, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarVertexOfBOnEdgeInteriorOfA_ReturnsPoint()
    {
        // Same configuration as above, with roles of A and B swapped.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(1, 3, 0); // lies on the edge from (4,0,0) to (0,4,0)
        var b1 = new Point(3, 5, 0);
        var b2 = new Point(1, 5, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarCollinearEdgesMeetingAtEndpoint_ReturnsPoint()
    {
        // Collinear edges that meet only at a single endpoint are a point contact,
        // not a segment overlap.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(2, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Non-coplanar: point and segment intersections
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_NonCoplanarPointIntersection_ReturnsPoint()
    {
        // Non-coplanar triangles intersect in exactly one point at (0,2,0).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(0, 2, 0);
        var a2 = new Point(2, 0, 0);

        var b0 = new Point(0, 2, 0);
        var b1 = new Point(0, 4, 0);
        var b2 = new Point(0, 2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 2, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarSegmentIntersection_ReturnsSegment()
    {
        // Non-coplanar triangles intersect along a segment on the y-axis.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarSegmentEqualsEdgeOfAAndSubsegmentOfB_ReturnsSegment()
    {
        // IntersectionTypes.segment is the full bottom edge of A, but only a subsegment
        // of a longer bottom edge of B.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 1, 0);

        var b0 = new Point(-1, 0, 0);
        var b1 = new Point(3, 0, 0);
        var b2 = new Point(-1, 0, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(-1, 0, 1));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarSegmentEqualsFullEdgeInBothTriangles_ReturnsSegment()
    {
        // IntersectionTypes.segment is a shared full edge between non-coplanar triangles.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 1, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(2, 0, 0);
        var b2 = new Point(0, 0, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, -1, 1));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarSharedVertexOnly_ReturnsPoint()
    {
        // Triangles share only the origin; their planes are different.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(0, 2, 2);
        var b2 = new Point(0, -2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(IntersectionTypes.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Non-coplanar: no intersection (None)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_NonCoplanarParallelPlanesDisjoint_ReturnsNone()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(0, 0, 4);
        var b1 = new Point(2, 0, 4);
        var b2 = new Point(0, 2, 4);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 5));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarVertexOnPlaneOutsideOtherTriangle_ReturnsNone()
    {
        var a0 = new Point(0, 10, 10);
        var a1 = new Point(2, 10, 10);
        var a2 = new Point(0, 11, 10);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(0, 10, 0);
        var b2 = new Point(0, 0, -10);

        var triA = new Triangle(a0, a1, a2, new Point(0, 10, 11));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarStraddlingButNoOverlap_ReturnsNone()
    {
        // Both triangles cross each other's planes, but the clipped intervals do not overlap.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 10, -1);
        var b1 = new Point(0, 10, 1);
        var b2 = new Point(0, 12, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 10, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = IntersectionTypes.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(IntersectionTypes.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Symmetry under swapping triangle order
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarArea_SymmetricUnderSwap()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kindAB = IntersectionTypes.Classify(in triA, in triB);
        var kindBA = IntersectionTypes.Classify(in triB, in triA);

        Assert.Equal(IntersectionType.Area, kindAB);
        Assert.Equal(kindAB, kindBA);
    }

    [Fact]
    public void Classify_Segment_SymmetricUnderSwap()
    {
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kindAB = IntersectionTypes.Classify(in triA, in triB);
        var kindBA = IntersectionTypes.Classify(in triB, in triA);

        Assert.Equal(IntersectionType.Segment, kindAB);
        Assert.Equal(kindAB, kindBA);
    }

    [Fact]
    public void Classify_Point_SymmetricUnderSwap()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(0, 2, 0);
        var a2 = new Point(2, 0, 0);

        var b0 = new Point(0, 2, 0);
        var b1 = new Point(0, 4, 0);
        var b2 = new Point(0, 2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 2, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kindAB = IntersectionTypes.Classify(in triA, in triB);
        var kindBA = IntersectionTypes.Classify(in triB, in triA);

        Assert.Equal(IntersectionType.Point, kindAB);
        Assert.Equal(kindAB, kindBA);
    }

    [Fact]
    public void Classify_None_SymmetricUnderSwap()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(4, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kindAB = IntersectionTypes.Classify(in triA, in triB);
        var kindBA = IntersectionTypes.Classify(in triB, in triA);

        Assert.Equal(IntersectionType.None, kindAB);
        Assert.Equal(kindAB, kindBA);
    }
    // ----------------------------------------------------------------------
    // Orientation / normal variations
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarOrientationFlips_DoNotChangeResult()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        var kindBase = IntersectionTypes.Classify(in triA, in triB);

        var triAFlipped = new Triangle(a0, a2, a1, new Point(0, 0, 1));
        var triBFlipped = new Triangle(b0, b2, b1, new Point(0, 0, 1));

        var kindAFlipped = IntersectionTypes.Classify(in triAFlipped, in triB);
        var kindBFlipped = IntersectionTypes.Classify(in triA, in triBFlipped);
        var kindBothFlipped = IntersectionTypes.Classify(in triAFlipped, in triBFlipped);

        Assert.Equal(kindBase, kindAFlipped);
        Assert.Equal(kindBase, kindBFlipped);
        Assert.Equal(kindBase, kindBothFlipped);
    }

    [Fact]
    public void Classify_NonCoplanarOrientationFlips_DoNotChangeResult()
    {
        // Base configuration: non-coplanar segment intersection along y-axis.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kindBase = IntersectionTypes.Classify(in triA, in triB);

        var triAFlipped = new Triangle(a0, a2, a1, new Point(0, 0, 1));
        var triBFlipped = new Triangle(b0, b2, b1, new Point(1, 0, 0));

        var kindAFlipped = IntersectionTypes.Classify(in triAFlipped, in triB);
        var kindBFlipped = IntersectionTypes.Classify(in triA, in triBFlipped);
        var kindBothFlipped = IntersectionTypes.Classify(in triAFlipped, in triBFlipped);

        Assert.Equal(kindBase, kindAFlipped);
        Assert.Equal(kindBase, kindBFlipped);
        Assert.Equal(kindBase, kindBothFlipped);
    }

    // ----------------------------------------------------------------------
    // Robustness to varying missing points (normal orientation)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarArea_InvariantUnderMissingPointChanges()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triAUp = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triBUp = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        var triADown = new Triangle(a0, a1, a2, new Point(0, 0, -5));
        var triBDown = new Triangle(b0, b1, b2, new Point(0, 0, -5));

        Assert.True(TrianglePredicates.IsCoplanar(in triAUp, in triBUp));
        Assert.True(TrianglePredicates.IsCoplanar(in triADown, in triBDown));

        var kindUp = IntersectionTypes.Classify(in triAUp, in triBUp);
        var kindDown = IntersectionTypes.Classify(in triADown, in triBDown);

        Assert.Equal(IntersectionType.Area, kindUp);
        Assert.Equal(kindUp, kindDown);
    }

    [Fact]
    public void Classify_NonCoplanarSegment_InvariantUnderMissingPointChanges()
    {
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        var triAFlippedNormal = new Triangle(a0, a1, a2, new Point(0, 0, -5));
        var triBFlippedNormal = new Triangle(b0, b1, b2, new Point(-2, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.IsCoplanar(in triAFlippedNormal, in triBFlippedNormal));

        var kindBase = IntersectionTypes.Classify(in triA, in triB);
        var kindFlippedNormal = IntersectionTypes.Classify(in triAFlippedNormal, in triBFlippedNormal);

        Assert.Equal(IntersectionType.Segment, kindBase);
        Assert.Equal(kindBase, kindFlippedNormal);
    }
}


