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

                bool exists = working.Exists(tri => Geometry2DIntersections.EdgeMatchesTriangle(edge, tri));

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

                foreach (var kvp in adjacency)
                {
                    if (kvp.Value.Count != 2)
                    {
                        throw new InvalidOperationException("Corridor boundary is not a simple cycle.");
                    }
                }

                var ring = new List<int>();
                ring.Add(a);
                int prev = b;
                int current = a;
                int next = b;
                ring.Add(next);
                while (true)
                {
                    var neighbors = adjacency[next];
                    int candidate = neighbors[0] == prev ? neighbors[1] : neighbors[0];
                    prev = next;
                    next = candidate;
                    if (next == a)
                    {
                        break;
                    }
                    ring.Add(next);
                }

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

    }
}
