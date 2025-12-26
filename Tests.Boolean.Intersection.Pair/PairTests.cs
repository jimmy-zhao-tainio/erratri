using System;
using System.Collections.Generic;
using Geometry;
using Boolean;
using Geometry.Topology;
using Xunit;

namespace Tests.Boolean.Intersection.Pair;

public class PairTests
{
    [Fact]
    public void Constructor_NoOverlap_ReturnsEmptyIntersections()
    {
        var a0 = new Triangle(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 1));
        var b0 = new Triangle(
            new Point(10, 0, 0),
            new Point(12, 0, 0),
            new Point(10, 2, 0),
            new Point(10, 0, 1));
        var trianglesA = new List<Triangle> { a0 };
        var trianglesB = new List<Triangle> { b0 };
        var set = new IntersectionSet(trianglesA, trianglesB);
        Assert.Empty(set.Intersections);
    }

    [Fact]
    public void Constructor_SinglePointIntersection_ReturnsOneEntry()
    {
        // Coplanar triangles sharing only the origin as a vertex.
        var a0 = new Triangle(
            new Point(0, 0, 0),
            new Point(4, 0, 0),
            new Point(0, 4, 0),
            new Point(0, 0, 1));
        var b0 = new Triangle(
            new Point(0, 0, 0),
            new Point(-4, 0, 0),
            new Point(0, -4, 0),
            new Point(0, 0, 1));
        var trianglesA = new List<Triangle> { a0 };
        var trianglesB = new List<Triangle> { b0 };
        var set = new IntersectionSet(trianglesA, trianglesB);
        Assert.Single(set.Intersections);
        var ti = set.Intersections[0];
        Assert.Equal(0, ti.TriangleIndexA);
        Assert.Equal(0, ti.TriangleIndexB);
        Assert.Equal(IntersectionType.Point, ti.Type);
    }

    [Fact]
    public void Constructor_SingleSegmentIntersection_ReturnsOneEntry()
    {
        // Non-coplanar triangles intersecting along a segment on the y-axis.
        var triA = new Triangle(
            new Point(0, -1, 0),
            new Point(0, 1, 0),
            new Point(1, 0, 0),
            new Point(0, 0, 1));
        var triB = new Triangle(
            new Point(0, 0, -1),
            new Point(0, 0, 1),
            new Point(0, 2, 0),
            new Point(1, 0, 0));
        var trianglesA = new List<Triangle> { triA };
        var trianglesB = new List<Triangle> { triB };
        var set = new IntersectionSet(trianglesA, trianglesB);
        Assert.Single(set.Intersections);
        var ti = set.Intersections[0];
        Assert.Equal(0, ti.TriangleIndexA);
        Assert.Equal(0, ti.TriangleIndexB);
        Assert.Equal(IntersectionType.Segment, ti.Type);
    }

    [Fact]
    public void Constructor_MultipleTriangles_ReturnsExpectedPairs()
    {
        // Area intersection near origin.
        var a0 = new Triangle(
            new Point(0, 0, 0),
            new Point(6, 0, 0),
            new Point(0, 6, 0),
            new Point(0, 0, 1));
        var b1 = new Triangle(
            new Point(1, 1, 0),
            new Point(4, 1, 0),
            new Point(1, 4, 0),
            new Point(0, 0, 1));
        // Point intersection far away around x=100.
        var a1 = new Triangle(
            new Point(100, 0, 0),
            new Point(104, 0, 0),
            new Point(100, 4, 0),
            new Point(100, 0, 1));
        var b2 = new Triangle(
            new Point(100, 0, 0),
            new Point(96, 0, 0),
            new Point(100, -4, 0),
            new Point(100, 0, 1));
        // Disjoint triangle that should be culled by the bounding box tree.
        var b0 = new Triangle(
            new Point(20, 0, 0),
            new Point(22, 0, 0),
            new Point(20, 2, 0),
            new Point(20, 0, 1));
        var trianglesA = new List<Triangle> { a0, a1 };
        var trianglesB = new List<Triangle> { b0, b1, b2 };
        var set = new IntersectionSet(trianglesA, trianglesB);
        var intersections = new List<IntersectionSet.Intersection>(set.Intersections);
        intersections.Sort((x, y) =>
        {
            var cmp = x.TriangleIndexA.CompareTo(y.TriangleIndexA);
            if (cmp != 0) return cmp;
            return x.TriangleIndexB.CompareTo(y.TriangleIndexB);
        });
        Assert.Equal(2, intersections.Count);
        Assert.Equal(0, intersections[0].TriangleIndexA);
        Assert.Equal(1, intersections[0].TriangleIndexB);
        Assert.Equal(IntersectionType.Area, intersections[0].Type);
        Assert.Equal(1, intersections[1].TriangleIndexA);
        Assert.Equal(2, intersections[1].TriangleIndexB);
        Assert.Equal(IntersectionType.Point, intersections[1].Type);
    }
}


