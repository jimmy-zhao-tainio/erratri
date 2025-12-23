using System;
using System.Collections.Generic;
using Geometry;
using Boolean;
using Geometry.Topology;
using Xunit;

namespace Tests.Boolean.Intersection.Graph;

public class GraphFromIntersectionSetTests
{
    [Fact]
    public void FromIntersectionSet_NullTrianglesA_Throws()
    {
        var set = default(IntersectionSet);
        Assert.Throws<ArgumentNullException>(() => IntersectionGraph.FromIntersectionSet(set));
    }

    [Fact]
    public void FromIntersectionSet_EmptySet_HasEmptyVerticesAndEdges()
    {
        var trianglesA = new List<Triangle>();
        var trianglesB = new List<Triangle>();
        var set = new IntersectionSet(trianglesA, trianglesB);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        Assert.Same(set.TrianglesA, graph.IntersectionSet.TrianglesA);
        Assert.Same(set.TrianglesB, graph.IntersectionSet.TrianglesB);
        Assert.Equal(set.Intersections.Count, graph.IntersectionSet.Intersections.Count);
        Assert.Empty(graph.Vertices);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void FromIntersectionSet_ValidSet_RetainsReference()
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
        var graph = IntersectionGraph.FromIntersectionSet(set);
        Assert.Same(set.TrianglesA, graph.IntersectionSet.TrianglesA);
        Assert.Same(set.TrianglesB, graph.IntersectionSet.TrianglesB);
        Assert.Equal(set.Intersections.Count, graph.IntersectionSet.Intersections.Count);
    }
}


