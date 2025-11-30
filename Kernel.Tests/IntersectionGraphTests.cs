using System.Collections.Generic;
using Geometry;
using Kernel;
using Topology;
using World;
using Xunit;

namespace Kernel.Tests;

public class IntersectionGraphTests
{
    [Fact]
    public void FromIntersectionSet_EmptyIntersections_WrapsSetAndHasNoGraphElements()
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

        var graph = IntersectionGraph.FromIntersectionSet(set);

        Assert.Same(set.TrianglesA, graph.IntersectionSet.TrianglesA);
        Assert.Same(set.TrianglesB, graph.IntersectionSet.TrianglesB);
        Assert.Equal(set.Intersections.Count, graph.IntersectionSet.Intersections.Count);

        Assert.Empty(graph.Vertices);
        Assert.Empty(graph.Edges);
        Assert.Empty(graph.Pairs);
    }

    [Fact]
    public void FromIntersectionSet_PairsMatchIntersections()
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

        var graph = IntersectionGraph.FromIntersectionSet(set);

        Assert.Equal(set.Intersections.Count, graph.Pairs.Count);

        for (int i = 0; i < set.Intersections.Count; i++)
        {
            var intersection = set.Intersections[i];
            var pair = graph.Pairs[i];

            Assert.Equal(intersection.TriangleIndexA, pair.Intersection.TriangleIndexA);
            Assert.Equal(intersection.TriangleIndexB, pair.Intersection.TriangleIndexB);
            Assert.Equal(intersection.Type, pair.Intersection.Type);
        }
    }

    [Fact]
    public void FromIntersectionSet_BuildsSingleSegmentGraph()
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
        Assert.Equal(IntersectionType.Segment, set.Intersections[0].Type);

        var graph = IntersectionGraph.FromIntersectionSet(set);

        Assert.Single(graph.Pairs);
        Assert.Equal(IntersectionType.Segment, graph.Pairs[0].Intersection.Type);

        Assert.Equal(2, graph.Vertices.Count);
        Assert.Single(graph.Edges);

        var edge = graph.Edges[0];
        Assert.NotEqual(edge.Start.Value, edge.End.Value);
    }

    [Fact]
    public void FromIntersectionSet_DeduplicatesSharedGlobalVertexAcrossPairs()
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

        var trianglesA = new List<Triangle> { a0, a0 };
        var trianglesB = new List<Triangle> { b0, b0 };

        var set = new IntersectionSet(trianglesA, trianglesB);

        Assert.True(set.Intersections.Count >= 2);
        foreach (var intersection in set.Intersections)
        {
            Assert.Equal(IntersectionType.Point, intersection.Type);
        }

        var graph = IntersectionGraph.FromIntersectionSet(set);

        Assert.Single(graph.Vertices);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void TriangleIntersectionIndex_SegmentCase_BuildsPerTriangleVertices()
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
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);

        Assert.Single(index.TrianglesA);
        Assert.Single(index.TrianglesB);

        var vertsA = index.TrianglesA[0];
        var vertsB = index.TrianglesB[0];

        Assert.Equal(2, vertsA.Length);
        Assert.Equal(2, vertsB.Length);

        // All per-triangle vertex ids must come from the global vertex set.
        var globalIds = new HashSet<int>();
        foreach (var (id, _) in graph.Vertices)
        {
            globalIds.Add(id.Value);
        }

        foreach (var v in vertsA)
        {
            Assert.Contains(v.VertexId.Value, globalIds);
        }

        foreach (var v in vertsB)
        {
            Assert.Contains(v.VertexId.Value, globalIds);
        }

    }

    [Fact]
    public void TriangleIntersectionIndex_DeduplicatesSharedPointAcrossTriangles()
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

        var trianglesA = new List<Triangle> { a0, a0 };
        var trianglesB = new List<Triangle> { b0, b0 };

        var set = new IntersectionSet(trianglesA, trianglesB);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);

        Assert.Single(graph.Vertices);
        var sharedId = graph.Vertices[0].Id;

        Assert.Equal(2, index.TrianglesA.Count);
        Assert.Equal(2, index.TrianglesB.Count);

        foreach (var verts in index.TrianglesA)
        {
            Assert.Single(verts);
            Assert.Equal(sharedId.Value, verts[0].VertexId.Value);
        }

        foreach (var verts in index.TrianglesB)
        {
            Assert.Single(verts);
            Assert.Equal(sharedId.Value, verts[0].VertexId.Value);
        }
    }

    [Fact]
    public void TriangleIntersectionIndex_SphereIntersection_AgreesWithIntersectionSet()
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var sphereA = new Sphere(r, subdivisions: 3, center: aCenter);
        var sphereB = new Sphere(r, subdivisions: 3, center: bCenter);

        var set = new IntersectionSet(
            sphereA.Mesh.Triangles,
            sphereB.Mesh.Triangles);

        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);

        var trianglesA = set.TrianglesA;
        var trianglesB = set.TrianglesB;

        // Triangles marked as intersecting in the set.
        var involvedA = new HashSet<int>();
        var involvedB = new HashSet<int>();
        foreach (var intersection in set.Intersections)
        {
            involvedA.Add(intersection.TriangleIndexA);
            involvedB.Add(intersection.TriangleIndexB);
        }

        // Every involved triangle must have at least one intersection vertex.
        foreach (var ai in involvedA)
        {
            Assert.NotEmpty(index.TrianglesA[ai]);
        }

        foreach (var bi in involvedB)
        {
            Assert.NotEmpty(index.TrianglesB[bi]);
        }

        // No triangle outside the involved sets should have intersection vertices.
        for (int i = 0; i < index.TrianglesA.Count; i++)
        {
            if (index.TrianglesA[i].Length > 0)
            {
                Assert.Contains(i, involvedA);
            }
        }

        for (int i = 0; i < index.TrianglesB.Count; i++)
        {
            if (index.TrianglesB[i].Length > 0)
            {
                Assert.Contains(i, involvedB);
            }
        }

        // Every per-triangle vertex should map back to a global vertex whose
        // world position matches the barycentric reconstruction on that triangle
        // within a small tolerance.
        var globalPositions = new Dictionary<int, RealPoint>();
        foreach (var (id, position) in graph.Vertices)
        {
            globalPositions[id.Value] = position;
        }

        const double tol = 1e-6;

        for (int i = 0; i < index.TrianglesA.Count; i++)
        {
            var tri = trianglesA[i];
            var verts = index.TrianglesA[i];
            for (int j = 0; j < verts.Length; j++)
            {
                var v = verts[j];
                Assert.True(globalPositions.TryGetValue(v.VertexId.Value, out var global));

                var bary = v.Barycentric;
                var world = Barycentric.ToRealPointOnTriangle(in tri, in bary);
                var dist2 = world.DistanceSquared(in global);
                Assert.True(dist2 <= tol * tol);
            }
        }

        for (int i = 0; i < index.TrianglesB.Count; i++)
        {
            var tri = trianglesB[i];
            var verts = index.TrianglesB[i];
            for (int j = 0; j < verts.Length; j++)
            {
                var v = verts[j];
                Assert.True(globalPositions.TryGetValue(v.VertexId.Value, out var global));

                var bary = v.Barycentric;
                var world = Barycentric.ToRealPointOnTriangle(in tri, in bary);
                var dist2 = world.DistanceSquared(in global);
                Assert.True(dist2 <= tol * tol);
            }
        }

        // Build mesh-local topologies (per-triangle edges, vertex adjacency, loops),
        // then run the intersection curve regularizer on mesh A and mesh B.
        var meshATopology = MeshATopology.Build(graph, index);
        var meshBTopology = MeshBTopology.Build(graph, index);
        var regularizationA = IntersectionCurveRegularizer.RegularizeMeshA(graph, meshATopology);

        // For the sphere-sphere case we expect at least one regularized
        // intersection curve on each mesh whose vertices form a closed cycle
        // with internal degree 2.
        Assert.NotEmpty(regularizationA.Curves);

        bool foundValidCurve = false;
        double longestLengthA = 0.0;

        foreach (var curve in regularizationA.Curves)
        {
            if (!IsClosedTwoRegular(curve))
            {
                continue;
            }

            foundValidCurve = true;
            if (curve.TotalLength > longestLengthA)
            {
                longestLengthA = curve.TotalLength;
            }
        }

        Assert.True(foundValidCurve);

        var regularizationB = IntersectionCurveRegularizer.RegularizeMeshB(graph, meshBTopology);
        Assert.NotEmpty(regularizationB.Curves);

        foundValidCurve = false;
        double longestLengthB = 0.0;

        foreach (var curve in regularizationB.Curves)
        {
            if (!IsClosedTwoRegular(curve))
            {
                continue;
            }

            foundValidCurve = true;
            if (curve.TotalLength > longestLengthB)
            {
                longestLengthB = curve.TotalLength;
            }
        }

        Assert.True(foundValidCurve);

        // The primary intersection loop on A and B should have nearly identical length.
        Assert.True(longestLengthA > 0.0);
        Assert.True(longestLengthB > 0.0);

        const double lengthTol = 1e-6;
        var diff = System.Math.Abs(longestLengthA - longestLengthB);
        var relDiff = diff / System.Math.Max(longestLengthA, longestLengthB);
        Assert.True(relDiff <= lengthTol);
    }

    private static bool IsClosedTwoRegular(IntersectionCurve curve)
    {
        if (curve.Vertices.Length < 3)
        {
            return false;
        }

        if (curve.Vertices[0].Value != curve.Vertices[^1].Value)
        {
            return false;
        }

        var degree = new Dictionary<int, int>();

        for (int i = 0; i < curve.Vertices.Length - 1; i++)
        {
            int a = curve.Vertices[i].Value;
            int b = curve.Vertices[i + 1].Value;

            degree.TryGetValue(a, out var da);
            degree[a] = da + 1;

            degree.TryGetValue(b, out var db);
            degree[b] = db + 1;
        }

        foreach (var kvp in degree)
        {
            if (kvp.Value != 2)
            {
                return false;
            }
        }

        return true;
    }
}
