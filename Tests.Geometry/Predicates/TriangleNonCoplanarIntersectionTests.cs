using Geometry.Predicates;
using Xunit;

namespace Tests.Geometry.Predicates;

public class TriangleNonCoplanarIntersectionTests
{
    [Fact]
    public void ClassifyNonCoplanar_DisjointTriangles_ReturnsNone()
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

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.None, intersection.Type);
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_PointIntersection_ReturnsPoint()
    {
        // Triangles intersect in exactly one point at (0,2,0).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(0, 2, 0);
        var a2 = new Point(2, 0, 0);

        var b0 = new Point(0, 2, 0);
        var b1 = new Point(0, 4, 0);
        var b2 = new Point(0, 2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 2, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.Point, intersection.Type);
        Assert.True(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_SegmentIntersection_ReturnsSegment()
    {
        // Intersection is a segment along the y-axis from (0,0,0) to (0,1,0).
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.Segment, intersection.Type);
        Assert.True(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_SymmetricUnderSwap_ReturnsSameType()
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

        var intersectionAB = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        var intersectionBA = TrianglePredicates.ClassifyNonCoplanar(in triB, in triA);

        Assert.Equal(intersectionAB.Type, intersectionBA.Type);
    }

    [Fact]
    public void ClassifyNonCoplanar_VertexOnPlaneButOutsideOtherTriangle_NoIntersection()
    {
        // Triangle A lies in plane z = 10; one vertex lies on plane of B (x = 0),
        // but that vertex is outside the area of B. The triangles do not intersect.
        var a0 = new Point(0, 10, 10);
        var a1 = new Point(2, 10, 10);
        var a2 = new Point(0, 11, 10);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(0, 10, 0);
        var b2 = new Point(0, 0, -10);

        var triA = new Triangle(a0, a1, a2, new Point(0, 10, 11));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.None, intersection.Type);
        Assert.False(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_OrientationFlips_DoNotChangeResult()
    {
        // Base configuration: segment intersection along y-axis.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        var intersectionBase = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);

        // Flip winding of A and B separately; classification should not change.
        var triAFlipped = new Triangle(a0, a2, a1, new Point(0, 0, 1));
        var triBFlipped = new Triangle(b0, b2, b1, new Point(1, 0, 0));

        var intersectionAFlipped = TrianglePredicates.ClassifyNonCoplanar(in triAFlipped, in triB);
        var intersectionBFlipped = TrianglePredicates.ClassifyNonCoplanar(in triA, in triBFlipped);
        var intersectionBothFlipped = TrianglePredicates.ClassifyNonCoplanar(in triAFlipped, in triBFlipped);

        Assert.Equal(intersectionBase.Type, intersectionAFlipped.Type);
        Assert.Equal(intersectionBase.Type, intersectionBFlipped.Type);
        Assert.Equal(intersectionBase.Type, intersectionBothFlipped.Type);
    }

    [Fact]
    public void ClassifyNonCoplanar_StraddlingPlanesButNoOverlap_ReturnsNone()
    {
        // Both triangles cross each other's planes, but their clipped intervals
        // along the intersection line do not overlap, so there is no intersection.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 10, -1);
        var b1 = new Point(0, 10, 1);
        var b2 = new Point(0, 12, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 10, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.None, intersection.Type);
        Assert.False(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }
}

