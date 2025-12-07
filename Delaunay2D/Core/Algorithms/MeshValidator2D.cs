using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal static class MeshValidator2D
    {
        internal static void ValidateLocalMesh(
            IReadOnlyList<Triangle2D> triangles,
            IReadOnlyList<RealPoint2D> points,
            string context)
        {
            if (triangles is null) throw new ArgumentNullException(nameof(triangles));
            if (points is null) throw new ArgumentNullException(nameof(points));

            var edgeCounts = new Dictionary<Edge2D, int>();
            var seen = new HashSet<(int, int, int)>();

            foreach (var tri in triangles)
            {
                if (tri.A < 0 || tri.B < 0 || tri.C < 0 ||
                    tri.A >= points.Count || tri.B >= points.Count || tri.C >= points.Count)
                {
                    throw new InvalidOperationException(
                        $"Triangle has vertex index out of range in {context}: " +
                        $"({tri.A},{tri.B},{tri.C}), points.Count={points.Count}.");
                }

                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];
                double area = 0.5 * ((pb.X - pa.X) * (pc.Y - pa.Y) - (pb.Y - pa.Y) * (pc.X - pa.X));
                if (area <= Geometry2DPredicates.Epsilon)
                {
                    throw new InvalidOperationException(
                        $"Non-positive or degenerate triangle area in {context}: " +
                        $"({tri.A},{tri.B},{tri.C}), area={area}.");
                }

                var key = SortedKey(tri);
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate triangle detected in {context}: " +
                        $"({tri.A},{tri.B},{tri.C}) as key ({key.Item1},{key.Item2},{key.Item3}).");
                }

                AccumulateEdge(edgeCounts, tri.A, tri.B);
                AccumulateEdge(edgeCounts, tri.B, tri.C);
                AccumulateEdge(edgeCounts, tri.C, tri.A);
            }

            foreach (var kvp in edgeCounts)
            {
                var edge = kvp.Key;
                int count = kvp.Value;
                if (count != 1 && count != 2)
                {
                    throw new InvalidOperationException(
                        $"Non-manifold edge in {context}: edge=({edge.A},{edge.B}), incidentTriangles={count}.");
                }
            }
        }

        private static (int, int, int) SortedKey(Triangle2D tri)
        {
            int a = tri.A;
            int b = tri.B;
            int c = tri.C;
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);
            return (a, b, c);
        }

        private static void AccumulateEdge(Dictionary<Edge2D, int> counts, int a, int b)
        {
            var e = new Edge2D(a, b);
            counts.TryGetValue(e, out var c);
            counts[e] = c + 1;
        }
    }
}
