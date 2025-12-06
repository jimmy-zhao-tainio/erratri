using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal static class ConstraintEnforcer2D
    {
        internal static List<Triangle2D> EnforceSegments(
            IReadOnlyList<RealPoint2D> points,
            List<Triangle2D> triangles,
            IReadOnlyList<(int A, int B)> segments)
        {
            if (segments.Count == 0)
            {
                return triangles;
            }

            var working = new List<Triangle2D>(triangles);
            var constrainedEdges = new HashSet<Edge2D>();

            foreach (var seg in segments)
            {
                int a = seg.A;
                int b = seg.B;
                if (a == b || a < 0 || b < 0 || a >= points.Count || b >= points.Count)
                {
                    throw new ArgumentException("Constraint segment indices must be distinct and within points range.");
                }

                var edge = new Edge2D(a, b);
                if (constrainedEdges.Contains(edge))
                {
                    continue;
                }

                bool exists = working.Exists(tri => Geometry2DIntersections.TriangleHasUndirectedEdge(tri, a, b));

                if (exists)
                {
                    constrainedEdges.Add(edge);
                    continue;
                }

                // Remove intersected triangles.
                var corridor = new List<Triangle2D>();
                var remaining = new List<Triangle2D>();
                foreach (var tri in working)
                {
                    if (Geometry2DIntersections.SegmentIntersectsTriangle(points, edge, tri))
                    {
                        corridor.Add(tri);
                    }
                    else
                    {
                        remaining.Add(tri);
                    }
                }

                if (corridor.Count == 0)
                {
                    throw new InvalidOperationException($"Failed to find corridor for constraint edge ({a},{b}).");
                }

                EnsureCorridorIsSingleComponent(corridor, a, b);

                // Boundary edges of corridor.
                var edgeCounts = new Dictionary<Edge2D, int>();
                foreach (var tri in corridor)
                {
                    Accumulate(edgeCounts, new Edge2D(tri.A, tri.B));
                    Accumulate(edgeCounts, new Edge2D(tri.B, tri.C));
                    Accumulate(edgeCounts, new Edge2D(tri.C, tri.A));
                }

                var boundaryEdges = new List<Edge2D>();
                foreach (var kvp in edgeCounts)
                {
                    if (kvp.Value == 1)
                    {
                        boundaryEdges.Add(kvp.Key);
                    }
                }

                if (!boundaryEdges.Contains(edge))
                {
                    boundaryEdges.Add(edge);
                }

                // Build adjacency and ring.
                var adjacency = new Dictionary<int, List<int>>();
                foreach (var e in boundaryEdges)
                {
                    if (!adjacency.TryGetValue(e.A, out var listA))
                    {
                        listA = new List<int>();
                        adjacency[e.A] = listA;
                    }
                    listA.Add(e.B);

                    if (!adjacency.TryGetValue(e.B, out var listB))
                    {
                        listB = new List<int>();
                        adjacency[e.B] = listB;
                    }
                    listB.Add(e.A);
                }

                var ring = BuildBoundaryRingOrThrow(a, b, adjacency);

                // Ensure CCW ring orientation for polygon triangulation.
                double ringArea = 0.0;
                for (int i = 0; i < ring.Count; i++)
                {
                    var p0 = points[ring[i]];
                    var p1 = points[ring[(i + 1) % ring.Count]];
                    ringArea += p0.X * p1.Y - p1.X * p0.Y;
                }
                if (ringArea < 0)
                {
                    ring.Reverse();
                }

                var newTriangles = PolygonTriangulator2D.TriangulateSimpleRing(ring, points);

                remaining.AddRange(newTriangles);
                working = remaining;
                EnsureConstrainedEdgePresent(working, a, b);
                constrainedEdges.Add(edge);
            }

            return working;
        }

        private static void Accumulate(Dictionary<Edge2D, int> edgeCounts, Edge2D edge)
        {
            if (edgeCounts.TryGetValue(edge, out int count))
            {
                edgeCounts[edge] = count + 1;
            }
            else
            {
                edgeCounts[edge] = 1;
            }
        }

        private static void EnsureCorridorIsSingleComponent(List<Triangle2D> corridor, int a, int b)
        {
            if (corridor.Count == 0)
            {
                return;
            }

            var edgeToTriangles = new Dictionary<Edge2D, List<int>>();
            for (int i = 0; i < corridor.Count; i++)
            {
                var tri = corridor[i];
                AddEdge(edgeToTriangles, new Edge2D(tri.A, tri.B), i);
                AddEdge(edgeToTriangles, new Edge2D(tri.B, tri.C), i);
                AddEdge(edgeToTriangles, new Edge2D(tri.C, tri.A), i);
            }

            var adjacency = new Dictionary<int, List<int>>();
            foreach (var kvp in edgeToTriangles)
            {
                var triIndices = kvp.Value;
                if (triIndices.Count < 2) continue;
                for (int i = 0; i < triIndices.Count; i++)
                {
                    for (int j = i + 1; j < triIndices.Count; j++)
                    {
                        AddNeighbor(adjacency, triIndices[i], triIndices[j]);
                        AddNeighbor(adjacency, triIndices[j], triIndices[i]);
                    }
                }
            }

            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            visited.Add(0);
            stack.Push(0);
            while (stack.Count > 0)
            {
                int current = stack.Pop();
                if (adjacency.TryGetValue(current, out var neighbors))
                {
                    foreach (var n in neighbors)
                    {
                        if (visited.Add(n))
                        {
                            stack.Push(n);
                        }
                    }
                }
            }

            if (visited.Count != corridor.Count)
            {
                throw new InvalidOperationException($"Constraint corridor is not a single connected component for edge ({a},{b}).");
            }
        }

        private static void AddEdge(Dictionary<Edge2D, List<int>> edgeToTriangles, Edge2D edge, int triIndex)
        {
            if (!edgeToTriangles.TryGetValue(edge, out var list))
            {
                list = new List<int>();
                edgeToTriangles[edge] = list;
            }
            list.Add(triIndex);
        }

        private static void AddNeighbor(Dictionary<int, List<int>> adjacency, int from, int to)
        {
            if (!adjacency.TryGetValue(from, out var list))
            {
                list = new List<int>();
                adjacency[from] = list;
            }
            if (!list.Contains(to))
            {
                list.Add(to);
            }
        }

        private static List<int> BuildBoundaryRingOrThrow(int a, int b, Dictionary<int, List<int>> adjacency)
        {
            foreach (var kvp in adjacency)
            {
                if (kvp.Value.Count != 2)
                {
                    throw new InvalidOperationException("Corridor boundary is not a simple cycle.");
                }
            }

            var ring = new List<int>();
            var visitedVertices = new HashSet<int>();
            var visitedEdges = new HashSet<(int, int)>();

            ring.Add(a);
            int prev = a;
            int current = b;
            ring.Add(current);

            while (true)
            {
                var neighbors = adjacency[current];
                int next = neighbors[0] == prev ? neighbors[1] : neighbors[0];

                var edge = NormalizeEdge(prev, current);
                if (!visitedEdges.Add(edge))
                {
                    throw new InvalidOperationException($"Constraint corridor boundary contains multiple cycles for edge ({a},{b}).");
                }

                if (next == a)
                {
                    break;
                }

                if (!visitedVertices.Add(current) && current != a)
                {
                    throw new InvalidOperationException($"Constraint corridor boundary contains multiple cycles for edge ({a},{b}).");
                }

                prev = current;
                current = next;
                ring.Add(current);
            }

            var lastEdge = NormalizeEdge(current, a);
            if (!visitedEdges.Add(lastEdge))
            {
                throw new InvalidOperationException($"Constraint corridor boundary contains multiple cycles for edge ({a},{b}).");
            }

            int abCount = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                int u = ring[i];
                int v = ring[(i + 1) % ring.Count];
                if ((u == a && v == b) || (u == b && v == a))
                {
                    abCount++;
                }
            }

            if (abCount == 0)
            {
                throw new InvalidOperationException($"Constraint corridor boundary does not contain the constrained edge ({a},{b}).");
            }
            if (abCount > 1)
            {
                throw new InvalidOperationException($"Constraint corridor boundary contains the constrained edge ({a},{b}) multiple times.");
            }

            return ring;
        }

        private static (int, int) NormalizeEdge(int u, int v) => u < v ? (u, v) : (v, u);

        private static void EnsureConstrainedEdgePresent(IReadOnlyList<Triangle2D> triangles, int a, int b)
        {
            foreach (var triangle in triangles)
            {
                if (Geometry2DIntersections.TriangleHasUndirectedEdge(triangle, a, b))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Constrained edge ({a},{b}) is missing after corridor re-triangulation.");
        }
    }
}
