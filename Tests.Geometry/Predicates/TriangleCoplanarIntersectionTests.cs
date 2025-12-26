using Geometry.Predicates;
using Xunit;

namespace Tests.Geometry.Predicates;

public class TriangleCoplanarIntersectionTests
{
    [Fact]
    public void ClassifyCoplanar_DisjointTriangles_ReturnsNone()
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

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.None, intersection.Type);
        Assert.False(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasPointIntersectionCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyCoplanar_AreaOverlap_SetsAreaFlag()
    {
        // Large base triangle
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);
        // Smaller triangle partially inside the large one
        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.Area, intersection.Type);
        Assert.True(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyCoplanar_SharedEdge_SetsSegmentFlag_NotArea()
    {
        // Two triangles sharing the edge from (0,0,0) to (4,0,0)
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);
        var b0 = new Point(0, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, -4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.Segment, intersection.Type);
        Assert.True(TrianglePredicates.HasSegmentIntersectionCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyCoplanar_SharedVertex_SetsPointFlag_NotArea()
    {
        // Two triangles sharing only the origin as a vertex
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);
        var b0 = new Point(0, 0, 0);
        var b1 = new Point(-4, 0, 0);
        var b2 = new Point(0, -4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        Assert.Equal(TriangleIntersectionType.Point, intersection.Type);
        Assert.False(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionCoplanar(in triA, in triB));
    }
}


