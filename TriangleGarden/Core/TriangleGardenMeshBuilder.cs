using System;
using System.Collections.Generic;
using Geometry;

namespace TriangleGarden
{
    internal static class TriangleGardenMeshBuilder
    {
        internal static List<(int A, int B, int C)> BuildTrianglesFromEdges(
            IReadOnlyList<(int A, int B)> segments,
            IReadOnlyList<RealPoint2D> points)
        {
            var adjacency = new Dictionary<int, HashSet<int>>();

            foreach (var seg in segments)
            {
                AddAdjacency(adjacency, seg.A, seg.B);
                AddAdjacency(adjacency, seg.B, seg.A);
            }

            var triangles = new HashSet<(int, int, int)>();
            foreach (var kvp in adjacency)
            {
                int a = kvp.Key;
                foreach (int b in kvp.Value)
                {
                    if (a < b && adjacency.TryGetValue(b, out var neighborsB))
                    {
                        foreach (int c in neighborsB)
                        {
                            if (b < c &&
                                adjacency.TryGetValue(c, out var neighborsC) &&
                                neighborsC.Contains(a))
                            {
                                if (Math.Abs(TriangleGardenGeometry.TriangleArea(points[a], points[b], points[c])) <= Tolerances.EpsArea)
                                {
                                    continue;
                                }

                                if (Enforce.ContainsInteriorPoint(a, b, c, points))
                                {
                                    continue;
                                }

                                triangles.Add(NormalizeTriangle(a, b, c));
                            }
                        }
                    }
                }
            }

            var result = new List<(int A, int B, int C)>(triangles.Count);
            foreach (var tri in triangles)
            {
                result.Add((tri.Item1, tri.Item2, tri.Item3));
            }
            return result;
        }

        private static void AddAdjacency(Dictionary<int, HashSet<int>> adjacency, int a, int b)
        {
            if (!adjacency.TryGetValue(a, out var set))
            {
                set = new HashSet<int>();
                adjacency[a] = set;
            }
            set.Add(b);
        }

        private static (int, int, int) NormalizeTriangle(int a, int b, int c)
        {
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);
            return (a, b, c);
        }
    }
}
