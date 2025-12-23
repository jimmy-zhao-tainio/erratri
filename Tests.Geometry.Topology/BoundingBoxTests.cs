using System.Collections.Generic;
using Geometry;
using Geometry.Topology;
using Xunit;

namespace Tests.Geometry.Topology;

public class BoundingBoxTests
{
    // ----------------------------------------------------------------------
    // BoundingBox basics
    // ----------------------------------------------------------------------

    [Fact]
    public void FromPoints_ComputesComponentwiseMinAndMax()
    {
        var a = new Point(1, 5, -2);
        var b = new Point(-3, 7, 4);
        var c = new Point(2, -1, 0);

        var box = BoundingBox.FromPoints(in a, in b, in c);

        Assert.Equal(new Point(-3, -1, -2), box.Min);
        Assert.Equal(new Point(2, 7, 4), box.Max);
    }

    [Fact]
    public void Union_ComputesMinimalEnclosingBox()
    {
        var a = new BoundingBox(new Point(0, 0, 0), new Point(2, 3, 4));
        var b = new BoundingBox(new Point(-1, 2, 1), new Point(5, 4, 6));

        var u = BoundingBox.Union(in a, in b);

        Assert.Equal(new Point(-1, 0, 0), u.Min);
        Assert.Equal(new Point(5, 4, 6), u.Max);
    }

    [Fact]
    public void Intersects_TouchingAtFaceEdgeOrPoint_ReturnsTrue()
    {
        var a = new BoundingBox(new Point(0, 0, 0), new Point(2, 2, 2));

        // Touch at a face (x = 2 plane)
        var face = new BoundingBox(new Point(2, 0, 0), new Point(4, 2, 2));
        Assert.True(a.Intersects(in face));
        Assert.True(face.Intersects(in a));

        // Touch at an edge
        var edge = new BoundingBox(new Point(2, 2, 0), new Point(4, 4, 2));
        Assert.True(a.Intersects(in edge));
        Assert.True(edge.Intersects(in a));

        // Touch at a single corner point
        var corner = new BoundingBox(new Point(2, 2, 2), new Point(4, 4, 4));
        Assert.True(a.Intersects(in corner));
        Assert.True(corner.Intersects(in a));
    }

    [Fact]
    public void Intersects_DisjointBoxes_ReturnsFalse()
    {
        var a = new BoundingBox(new Point(0, 0, 0), new Point(1, 1, 1));
        var b = new BoundingBox(new Point(2, 2, 2), new Point(3, 3, 3));

        Assert.False(a.Intersects(in b));
        Assert.False(b.Intersects(in a));
    }

    [Fact]
    public void Empty_DoesNotIntersectAnyNonEmptyBox()
    {
        var empty = BoundingBox.Empty;
        var box = new BoundingBox(new Point(0, 0, 0), new Point(1, 1, 1));

        Assert.False(empty.Intersects(in box));
        Assert.False(box.Intersects(in empty));
        Assert.True(empty.IsEmpty);
        Assert.False(box.IsEmpty);
    }
}

public class BoundingBoxTreeTests
{
    // ----------------------------------------------------------------------
    // BoundingBoxTree: basic queries
    // ----------------------------------------------------------------------

    [Fact]
    public void Query_SingleTriangleHit_ReturnsThatIndex()
    {
        var t0 = new Tetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        var t1 = new Tetrahedron(
            new Point(10, 0, 0),
            new Point(12, 0, 0),
            new Point(10, 2, 0),
            new Point(10, 0, 2));

        var t2 = new Tetrahedron(
            new Point(0, 0, 10),
            new Point(2, 0, 10),
            new Point(0, 2, 10),
            new Point(0, 0, 12));

        var triangles = new List<Triangle>
        {
            t0.ABC, // index 0
            t1.ABC, // index 1
            t2.ABC  // index 2
        };

        var tree = new BoundingBoxTree(triangles);

        // Query around the first triangle only.
        var query = new BoundingBox(new Point(-1, -1, -1), new Point(3, 3, 1));
        var hits = new List<int>();

        tree.Query(in query, hits);

        Assert.Single(hits);
        Assert.Equal(0, hits[0]);
    }

    [Fact]
    public void Query_MultipleTrianglesHit_ReturnsAllIndices()
    {
        var t0 = new Tetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        var t1 = new Tetrahedron(
            new Point(10, 0, 0),
            new Point(12, 0, 0),
            new Point(10, 2, 0),
            new Point(10, 0, 2));

        var t2 = new Tetrahedron(
            new Point(0, 0, 10),
            new Point(2, 0, 10),
            new Point(0, 2, 10),
            new Point(0, 0, 12));

        var triangles = new List<Triangle>
        {
            t0.ABC, // index 0
            t1.ABC, // index 1
            t2.ABC  // index 2
        };

        var tree = new BoundingBoxTree(triangles);

        // Query spans both triangles 0 and 1 in the z = 0 plane.
        var query = new BoundingBox(new Point(-1, -1, -1), new Point(20, 3, 1));
        var hits = new List<int>();

        tree.Query(in query, hits);

        hits.Sort();
        Assert.Equal(new[] { 0, 1 }, hits);
    }

    [Fact]
    public void Query_NoHits_ReturnsEmptyList()
    {
        var t0 = new Tetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        var t1 = new Tetrahedron(
            new Point(10, 0, 0),
            new Point(12, 0, 0),
            new Point(10, 2, 0),
            new Point(10, 0, 2));

        var triangles = new List<Triangle>
        {
            t0.ABC,
            t1.ABC
        };

        var tree = new BoundingBoxTree(triangles);

        var query = new BoundingBox(new Point(100, 100, 100), new Point(110, 110, 110));
        var hits = new List<int>();

        tree.Query(in query, hits);

        Assert.Empty(hits);
    }

    [Fact]
    public void Query_MoreThanLeafThreshold_StillReturnsCorrectIndices()
    {
        // Build more than 8 triangles so that the tree actually splits.
        var triangles = new List<Triangle>();
        for (int i = 0; i < 10; i++)
        {
            var baseX = i * 10;
            var tetra = new Tetrahedron(
                new Point(baseX, 0, 0),
                new Point(baseX + 2, 0, 0),
                new Point(baseX, 2, 0),
                new Point(baseX, 0, 2));
            triangles.Add(tetra.ABC);
        }

        var tree = new BoundingBoxTree(triangles);

        // Query around the triangles at indices 4, 5, and 6 (but not 7).
        var query = new BoundingBox(new Point(35, -1, -1), new Point(65, 3, 1));
        var hits = new List<int>();

        tree.Query(in query, hits);

        hits.Sort();
        Assert.Equal(new[] { 4, 5, 6 }, hits);
    }

    [Fact]
    public void Query_OverlappingBoxesOnBothSidesOfSplit_ReturnsAllIndices()
    {
        // Four overlapping triangle boxes arranged along the X axis.
        // Their centers lie on different sides of the split plane, but each
        // box spans that plane so the query must visit both left and right.
        var triangles = new List<Triangle>();
        for (int i = 0; i < 4; i++)
        {
            var baseX = i * 4;
            var tetra = new Tetrahedron(
                new Point(baseX, 0, 0),
                new Point(baseX + 8, 0, 0),
                new Point(baseX, 8, 0),
                new Point(baseX, 0, 8));
            triangles.Add(tetra.ABC);
        }

        var tree = new BoundingBoxTree(triangles);

        // Query straddles the central region, intersecting all four boxes.
        var query = new BoundingBox(new Point(6, -1, -1), new Point(14, 9, 9));
        var hits = new List<int>();

        tree.Query(in query, hits);

        hits.Sort();
        Assert.Equal(new[] { 0, 1, 2, 3 }, hits);
    }

    [Fact]
    public void Query_ManyIdenticalBoxes_ReturnsAllIndices()
    {
        // Many triangles with identical bounding boxes; tree will still partition
        // them, but every query overlapping that box must return all indices.
        const int count = 16;
        var triangles = new List<Triangle>();
        for (int i = 0; i < count; i++)
        {
            var tetra = new Tetrahedron(
                new Point(0, 0, 0),
                new Point(2, 0, 0),
                new Point(0, 2, 0),
                new Point(0, 0, 2));
            triangles.Add(tetra.ABC);
        }

        var tree = new BoundingBoxTree(triangles);

        var query = new BoundingBox(new Point(-1, -1, -1), new Point(3, 3, 3));
        var hits = new List<int>();

        tree.Query(in query, hits);

        hits.Sort();
        Assert.Equal(count, hits.Count);
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(i, hits[i]);
        }
    }

    [Fact]
    public void Query_EmptyInput_ReturnsEmptyResults()
    {
        var triangles = new List<Triangle>();
        var tree = new BoundingBoxTree(triangles);

        var query = new BoundingBox(new Point(0, 0, 0), new Point(10, 10, 10));
        var hits = new List<int>();

        tree.Query(in query, hits);

        Assert.Empty(hits);
    }
}


