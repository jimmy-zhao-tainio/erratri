using Geometry;
using Boolean;
using System;
using System.Collections.Generic;
using Geometry.Topology;
using Xunit;

namespace Tests.Boolean.Intersection.Pair;

public class PairFeaturesTests
{
    [Fact]
    public void PairFeatures_CreatedEmpty_HasNoVerticesOrSegments()
    {
        var intersection = new IntersectionSet.Intersection(
            triangleIndexA: 1,
            triangleIndexB: 2,
            type: IntersectionType.Segment);
        var features = PairFeaturesFactory.CreateEmpty(in intersection);
        Assert.Equal(1, features.Intersection.TriangleIndexA);
        Assert.Equal(2, features.Intersection.TriangleIndexB);
        Assert.Equal(IntersectionType.Segment, features.Intersection.Type);
        Assert.Empty(features.Vertices);
        Assert.Empty(features.Segments);
    }

    [Fact]
    public void PairVertex_StoresBarycentrics()
    {
        var vertexId = new IntersectionVertexId(5);
        var onA = new Barycentric(1, 0, 0);
        var onB = new Barycentric(0, 1, 0);
        var vertex = new PairVertex(vertexId, onA, onB);
        Assert.Equal(vertexId, vertex.VertexId);
        Assert.Equal(onA.U, vertex.OnTriangleA.U);
        Assert.Equal(onA.V, vertex.OnTriangleA.V);
        Assert.Equal(onA.W, vertex.OnTriangleA.W);
        Assert.Equal(onB.U, vertex.OnTriangleB.U);
        Assert.Equal(onB.V, vertex.OnTriangleB.V);
        Assert.Equal(onB.W, vertex.OnTriangleB.W);
    }

    [Fact]
    public void PairSegment_StoresEndpoints()
    {
        var id0 = new IntersectionVertexId(0);
        var id1 = new IntersectionVertexId(1);
        var v0 = new PairVertex(id0, new Barycentric(1, 0, 0), new Barycentric(0, 1, 0));
        var v1 = new PairVertex(id1, new Barycentric(0, 1, 0), new Barycentric(0, 0, 1));
        var segment = new PairSegment(v0, v1);
        Assert.Equal(v0, segment.Start);
        Assert.Equal(v1, segment.End);
    }

    [Fact]
    public void Create_NonCoplanarSegment_BuildsTwoVerticesAndOneSegment()
    {
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
        var intersection = set.Intersections[0];
        Assert.Equal(IntersectionType.Segment, intersection.Type);
        var features = PairFeaturesFactory.Create(in set, in intersection);
        Assert.Equal(IntersectionType.Segment, features.Intersection.Type);
        Assert.Equal(2, features.Vertices.Count);
        Assert.Single(features.Segments);
        foreach (var vertex in features.Vertices)
        {
            Assert.True(vertex.OnTriangleA.IsInsideInclusive(), "Vertex not inside triangle A.");
            Assert.True(vertex.OnTriangleB.IsInsideInclusive(), "Vertex not inside triangle B.");
        }
    }

    [Fact]
    public void Create_Point_BuildsSingleVertexNoSegments()
    {
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
        var intersection = set.Intersections[0];
        Assert.Equal(IntersectionType.Point, intersection.Type);
        var features = PairFeaturesFactory.Create(in set, in intersection);
        Assert.Equal(IntersectionType.Point, features.Intersection.Type);
        Assert.Single(features.Vertices);
        Assert.Empty(features.Segments);
        var vertex = features.Vertices[0];
        Assert.True(vertex.OnTriangleA.IsInsideInclusive(), "Vertex not inside triangle A.");
        Assert.True(vertex.OnTriangleB.IsInsideInclusive(), "Vertex not inside triangle B.");
    }

    [Fact]
    public void Create_AreaForIdenticalTriangles_BuildsPolygonLoop()
    {
        var a0 = new Triangle(
            new Point(0, 0, 0),
            new Point(6, 0, 0),
            new Point(0, 6, 0),
            new Point(0, 0, 1));
        // Identical geometry for B to force an Area intersection.
        var b0 = new Triangle(
            new Point(0, 0, 0),
            new Point(6, 0, 0),
            new Point(0, 6, 0),
            new Point(0, 0, 1));
        var trianglesA = new List<Triangle> { a0 };
        var trianglesB = new List<Triangle> { b0 };
        var set = new IntersectionSet(trianglesA, trianglesB);
        Assert.Single(set.Intersections);
        var intersection = set.Intersections[0];
        Assert.Equal(IntersectionType.Area, intersection.Type);
        var features = PairFeaturesFactory.Create(in set, in intersection);
        Assert.Equal(IntersectionType.Area, features.Intersection.Type);
        Assert.True(features.Vertices.Count >= 3);
        Assert.True(features.Segments.Count >= 3);
        // All segment endpoints should be drawn from the vertex set.
        foreach (var segment in features.Segments)
        {
            Assert.Contains(segment.Start, features.Vertices);
            Assert.Contains(segment.End, features.Vertices);
        }
    }

    [Fact]
    public void Create_SegmentTypeWithPointGeometry_DegeneratesToPoint()
    {
        // Geometry from the point-intersection case, but with
        // IntersectionType forced to Segment to exercise the
        // degradation path.
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
        var fakeIntersection = new IntersectionSet.Intersection(
            triangleIndexA: 0,
            triangleIndexB: 0,
            type: IntersectionType.Segment);
        var features = PairFeaturesFactory.Create(in set, in fakeIntersection);
        Assert.Equal(IntersectionType.Segment, features.Intersection.Type);
        Assert.Single(features.Vertices);
        Assert.Empty(features.Segments);
    }
}


