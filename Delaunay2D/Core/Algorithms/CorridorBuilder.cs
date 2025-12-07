using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    /// <summary>
    /// Topological description of a corridor patch for a constrained edge.
    /// Outer ring is a simple, non-self-intersecting cycle in CCW order containing the constrained edge exactly once.
    /// Inner rings (holes) are fully contained within the outer ring. For now, holes are not supported.
    /// Indices refer to the global points list.
    /// </summary>
    internal sealed class CorridorBoundary
    {
        public int[] OuterRing { get; }
        public IReadOnlyList<int[]> InnerRings { get; }

        public CorridorBoundary(int[] outerRing, IReadOnlyList<int[]> innerRings)
        {
            OuterRing = outerRing ?? throw new ArgumentNullException(nameof(outerRing));
            InnerRings = innerRings ?? throw new ArgumentNullException(nameof(innerRings));
        }
    }

    internal static class CorridorBuilder
    {
        /// <summary>
        /// Build the corridor for constrained edge AB and its boundary in the current mesh.
        /// Invariants (to be enforced by the eventual implementation):
        /// - OuterRing is a simple CCW cycle containing AB exactly once.
        /// - InnerRings are fully contained within OuterRing; for now holes are unsupported and must trigger an exception.
        /// - CorridorTriangles identify the set of triangles whose union matches the polygon defined by the rings.
        /// Current implementation supports multiple boundary cycles (outer + inner). Inner rings are surfaced on CorridorBoundary;
        /// ConstraintEnforcer2D currently rejects corridors with holes during re-triangulation.
        /// </summary>
        internal static (CorridorBoundary Boundary, IReadOnlyList<int> CorridorTriangles) BuildCorridor(
            MeshTopology2D mesh,
            IReadOnlyList<RealPoint2D> points,
            Edge2D ab)
        {
            if (mesh is null) throw new ArgumentNullException(nameof(mesh));
            if (points is null) throw new ArgumentNullException(nameof(points));

            // Replay existing single-ring logic using topology.
            var corridorTriangles = new List<int>();
            var remainingEdges = new Dictionary<Edge2D, int>();

            for (int i = 0; i < mesh.Triangles.Count; i++)
            {
                var tri = mesh.Triangles[i];
                var kind = Geometry2DIntersections.ClassifySegmentTriangleIntersection(points, ab, tri);
                switch (kind)
                {
                    case Geometry2DIntersections.SegmentTriangleIntersectionKind.ProperInterior:
                    case Geometry2DIntersections.SegmentTriangleIntersectionKind.EndpointInside:
                    case Geometry2DIntersections.SegmentTriangleIntersectionKind.CollinearOverlap:
                        corridorTriangles.Add(i);
                        Accumulate(remainingEdges, new Edge2D(tri.A, tri.B));
                        Accumulate(remainingEdges, new Edge2D(tri.B, tri.C));
                        Accumulate(remainingEdges, new Edge2D(tri.C, tri.A));
                        break;
                    default:
                        break;
                }
            }

            if (corridorTriangles.Count == 0)
            {
                throw new InvalidOperationException($"Failed to find corridor for constraint edge ({ab.A},{ab.B}).");
            }

            var boundaryEdges = new List<Edge2D>();
            foreach (var kvp in remainingEdges)
            {
                if (kvp.Value == 1)
                {
                    boundaryEdges.Add(kvp.Key);
                }
            }

            var adjacency = new Dictionary<int, List<int>>();
            foreach (var e in boundaryEdges)
            {
                if (!adjacency.TryGetValue(e.A, out var listA))
                {
                    listA = new List<int>();
                    adjacency[e.A] = listA;
                }
                if (!listA.Contains(e.B))
                {
                    listA.Add(e.B);
                }

                if (!adjacency.TryGetValue(e.B, out var listB))
                {
                    listB = new List<int>();
                    adjacency[e.B] = listB;
                }
                if (!listB.Contains(e.A))
                {
                    listB.Add(e.A);
                }
            }

            var boundary = BuildBoundaryMultiRing(ab.A, ab.B, adjacency, points);
            return (boundary, corridorTriangles);
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

        private static CorridorBoundary BuildBoundaryMultiRing(int a, int b, Dictionary<int, List<int>> adjacency, IReadOnlyList<RealPoint2D> points)
        {
            if (adjacency.Count == 0)
            {
                throw new InvalidOperationException("Corridor boundary is empty.");
            }

            foreach (var kvp in adjacency)
            {
                if (kvp.Value.Count != 2)
                {
                    throw new InvalidOperationException("Corridor boundary is not degree-2 at all vertices.");
                }
            }

            var visitedEdges = new HashSet<(int, int)>();
            var cycles = new List<List<int>>();

            foreach (var start in adjacency.Keys)
            {
                foreach (var n in adjacency[start])
                {
                    var e = NormalizeEdge(start, n);
                    if (visitedEdges.Contains(e))
                    {
                        continue;
                    }

                    var cycle = new List<int>();
                    int prev = start;
                    int current = n;
                    cycle.Add(prev);
                    cycle.Add(current);
                    visitedEdges.Add(e);

                    while (true)
                    {
                        var neighbors = adjacency[current];
                        int next = neighbors[0] == prev ? neighbors[1] : neighbors[0];
                        var edge = NormalizeEdge(current, next);
                        if (!visitedEdges.Add(edge))
                        {
                            break;
                        }
                        cycle.Add(next);
                        prev = current;
                        current = next;
                        if (current == start)
                        {
                            break;
                        }
                    }

                    cycles.Add(cycle);
                }
            }

            // Normalize cycles: drop duplicated closing vertex if present
            for (int i = 0; i < cycles.Count; i++)
            {
                var c = cycles[i];
                if (c.Count > 1 && c[0] == c[c.Count - 1])
                {
                    c.RemoveAt(c.Count - 1);
                }
            }

            // If there's only one boundary cycle, treat it as the outer ring with no holes.
            if (cycles.Count == 1)
            {
                var cycle = cycles[0];

                double area = SignedArea(points, cycle);

                // Outer ring should be CCW
                if (area < 0)
                {
                    cycle.Reverse();
                }

                return new CorridorBoundary(
                    cycle.ToArray(),
                    Array.Empty<int[]>());
            }

            List<int>? outer = null;
            var inner = new List<int[]>();
            int abOccurrences = 0;

            foreach (var cycle in cycles)
            {
                int abCount = 0;
                for (int i = 0; i < cycle.Count; i++)
                {
                    int u = cycle[i];
                    int v = cycle[(i + 1) % cycle.Count];
                    if ((u == a && v == b) || (u == b && v == a))
                    {
                        abCount++;
                    }
                }

                double area = SignedArea(points, cycle);

                if (abCount > 0)
                {
                    abOccurrences += abCount;
                    var candidate = new List<int>(cycle);
                    if (area < 0)
                    {
                        candidate.Reverse();
                    }
                    outer = candidate;
                }
                else
                {
                    var hole = new List<int>(cycle);
                    if (area > 0)
                    {
                        hole.Reverse(); // holes CW
                    }

                    if (Geometry2DIntersections.HasSelfIntersectionProper(hole, points))
                    {
                        throw new InvalidOperationException("Corridor boundary self-intersects in an inner ring.");
                    }

                    inner.Add(hole.ToArray());
                }
            }

            if (outer is null)
            {
                throw new InvalidOperationException($"Corridor boundary does not contain the constrained edge ({a},{b}).");
            }

            if (abOccurrences != 1)
            {
                throw new InvalidOperationException($"Corridor boundary contains the constrained edge ({a},{b}) multiple times.");
            }

            if (Geometry2DIntersections.HasSelfIntersectionProper(outer, points))
            {
                throw new InvalidOperationException($"Constraint corridor boundary self-intersects for edge ({a},{b}).");
            }

            return new CorridorBoundary(outer.ToArray(), inner);
        }

        private static (int, int) NormalizeEdge(int u, int v) => u < v ? (u, v) : (v, u);

        private static double SignedArea(IReadOnlyList<RealPoint2D> points, List<int> ring)
        {
            double area2 = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                var p0 = points[ring[i]];
                var p1 = points[ring[(i + 1) % ring.Count]];
                area2 += p0.X * p1.Y - p1.X * p0.Y;
            }
            return 0.5 * area2;
        }
    }
}
