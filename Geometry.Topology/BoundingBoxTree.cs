using System;
using System.Collections.Generic;
using Geometry;

namespace Geometry.Topology;

public sealed class BoundingBoxTree
{
    private readonly Node root;

    private readonly struct TriangleBox
    {
        public readonly int Index;
        public readonly BoundingBox Box;
        public TriangleBox(int i, in BoundingBox b) { Index = i; Box = b; }
    }

    private sealed class Node
    {
        public BoundingBox Box;
        public Node? Left;
        public Node? Right;
        public int Start;
        public int Count;
    }

    private readonly TriangleBox[] triangles;

    public BoundingBoxTree(IReadOnlyList<Triangle> triangles)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));

        if (triangles.Count == 0)
        {
            this.triangles = Array.Empty<TriangleBox>();
            root = new Node
            {
                Box = BoundingBox.Empty,
                Start = 0,
                Count = 0
            };
            return;
        }

        this.triangles = BuildTriangleBoxes(triangles);
        root = Build(0, this.triangles.Length);
    }

    public void Query(in BoundingBox box, List<int> results)
        => QueryNode(root, box, results);

    private static TriangleBox[] BuildTriangleBoxes(IReadOnlyList<Triangle> triangles)
    {
        var triangleBoxes = new TriangleBox[triangles.Count];
        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            var box = BoundingBox.FromPoints(triangle.P0, triangle.P1, triangle.P2);
            triangleBoxes[i] = new TriangleBox(i, box);
        }
        return triangleBoxes;
    }

    private Node Build(int start, int end)
    {
        var node = new Node();
        long minX = long.MaxValue, minY = long.MaxValue, minZ = long.MaxValue;
        long maxX = long.MinValue, maxY = long.MinValue, maxZ = long.MinValue;
        for (int i = start; i < end; i++)
        {
            var b = triangles[i].Box;
            if (b.Min.X < minX) minX = b.Min.X;
            if (b.Min.Y < minY) minY = b.Min.Y;
            if (b.Min.Z < minZ) minZ = b.Min.Z;
            if (b.Max.X > maxX) maxX = b.Max.X;
            if (b.Max.Y > maxY) maxY = b.Max.Y;
            if (b.Max.Z > maxZ) maxZ = b.Max.Z;
        }
        node.Box = new BoundingBox(new Point(minX, minY, minZ), new Point(maxX, maxY, maxZ));

        int count = end - start;
        if (count <= 8)
        {
            node.Start = start;
            node.Count = count;
            return node;
        }

        long ex = maxX - minX, ey = maxY - minY, ez = maxZ - minZ;
        int axis = ex >= ey && ex >= ez ? 0 : (ey >= ez ? 1 : 2);

        Array.Sort(triangles, start, count, new TriangleBoxComparer(axis));
        int mid = start + count / 2;
        node.Left = Build(start, mid);
        node.Right = Build(mid, end);
        return node;
    }

    private void QueryNode(Node node, in BoundingBox box, List<int> results)
    {
        if (!node.Box.Intersects(box)) return;
        if (node.Count > 0)
        {
            for (int i = 0; i < node.Count; i++)
            {
                var tb = triangles[node.Start + i];
                if (tb.Box.Intersects(box)) results.Add(tb.Index);
            }
            return;
        }
        if (node.Left is not null) QueryNode(node.Left, box, results);
        if (node.Right is not null) QueryNode(node.Right, box, results);
    }

    private sealed class TriangleBoxComparer : IComparer<TriangleBox>
    {
        private readonly int axis;
        public TriangleBoxComparer(int axis) { this.axis = axis; }
        public int Compare(TriangleBox x, TriangleBox y)
        {
            long cx = Center(x.Box, axis);
            long cy = Center(y.Box, axis);
            return cx.CompareTo(cy);
        }
        private static long Center(in BoundingBox b, int axis)
            => axis == 0 ? (b.Min.X + b.Max.X) : axis == 1 ? (b.Min.Y + b.Max.Y) : (b.Min.Z + b.Max.Z);
    }
}

