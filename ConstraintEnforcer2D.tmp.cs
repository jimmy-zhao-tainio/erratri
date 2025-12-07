using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal static class ConstraintEnforcer2D
    {
        /// <summary>
        /// Enforce constrained segments (a,b) on an existing triangulation.
        /// Cases:
        /// - If an undirected edge (a,b) already exists (strict index match, no epsilon), the constraint is treated as already satisfied.
        /// - Otherwise, triangles are classified via Geometry2DIntersections.ClassifySegmentTriangleIntersection:
        ///     * ProperInterior / EndpointInside → always enter the corridor.
        ///     * CollinearOverlap → joins the corridor only if there is at least one ProperInterior/EndpointInside hit.
        ///     * TouchVertex / TouchEdge → never enter the corridor; touch-only configurations do not build a corridor.
        /// - Pure collinear chains (no interior hits, at least one collinear overlap) throw with an explicit “collinear chains are not yet supported” message.
        /// Geometry2DPredicates.Epsilon is used inside the classifier to separate “overlap” vs “touching” for collinear and intersection tests; EnforceSegments itself performs only index-exact edge checks.
        /// </summary>
        internal static List<Triangle2D> EnforceSegments(
            IReadOnlyList<RealPoint2D> points,
            List<Triangle2D> triangles,
            IReadOnlyList<(int A, int B)> segments,
            Delaunay2DDebugOptions? debug = null,
            ConstraintEnforcer2DOptions? options = null)
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
                    throw new ArgumentException(Delaunay2DError.Format(
                        Delaunay2DErrorCategory.Input,
                        $"Constraint segment indices must be distinct and within points range: ({a},{b})."));
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

                var meshTopology = new ListBackedMeshTopology2D(working);
                IReadOnlyList<Triangle2D>? meshBeforeDump = null;
                bool dumpEnabled = debug?.EnableCorridorDump == true || debug?.EnableCorridorDumpsBeforeAfter == true;
                if (dumpEnabled)
                {
                    meshBeforeDump = meshTopology.Triangles.ToArray();
                }

                var (boundary, corridorTriangles) = CorridorBuilder.BuildCorridor(meshTopology, points, edge);

                var corridor = new List<Triangle2D>();
                var corridorKeys = new HashSet<(int, int, int)>();
                var remaining = new List<Triangle2D>();

                for (int i = 0; i < working.Count; i++)
                {
                    if (corridorTriangles.Contains(i))
                    {
                        var tri = working[i];
                        corridor.Add(tri);
                        corridorKeys.Add(SortedTriangleKey(tri));
                    }
                    else
                    {
                        remaining.Add(working[i]);
                    }
                }

                EnsureCorridorIsSingleComponent(corridor, a, b);

                var ring = new List<int>(boundary.OuterRing);

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

                List<Triangle2D> newTriangles;
                if (boundary.InnerRings.Count == 0)
                {
                    newTriangles = PolygonTriangulator2D.TriangulateSimpleRing(ring, points);
                }
                else
                {
                    newTriangles = PolygonTriangulator2D.TriangulateWithHoles(boundary.OuterRing, boundary.InnerRings, points);
                }
                if (boundary.InnerRings.Count == 0)
                {
                    int expected = ring.Count - 2;
                    if (newTriangles.Count != expected)
                    {
                        throw new InvalidOperationException(
                            Delaunay2DError.Format(
                                Delaunay2DErrorCategory.Numeric,
                                $"PolygonTriangulator2D returned {newTriangles.Count} triangles for corridor ring with {ring.Count} vertices for constraint edge ({a},{b}); expected {expected}."));
                    }
                }

                int startIndex = remaining.Count;
                remaining.AddRange(newTriangles);
                working = remaining;
                if (options?.RelaxToDelaunayAfterInsert == true)
                {
                    var patchIndices = new List<int>();
                    for (int i = startIndex; i < startIndex + newTriangles.Count; i++)
                    {
                        patchIndices.Add(i);
                    }

                    var frozen = new HashSet<Edge2D>(constrainedEdges) { edge };
                    LocalDelaunayRelaxation.Relax(points, working, patchIndices, frozen, log: dumpEnabled);
                }

                if (dumpEnabled)
                {
                    var meshAfter = working.ToArray();
                    var dump = new CorridorDump(
                        "Corridor",
                        new Edge2D(edge.A, edge.B),
                        boundary.OuterRing,
                        boundary.InnerRings,
                        corridorTriangles,
                        points.ToArray(),
                        meshBeforeDump ?? Array.Empty<Triangle2D>(),
                        meshAfter);
                    var handler = debug.CorridorDumpHandler ?? DefaultCorridorDumpHandler;
                    handler(dump);
                }
                MeshValidator2D.ValidateLocalMesh(
                    working,
                    points,
                    $"after enforcing constraint ({a},{b})");
                foreach (var tri in working)
                {
                    var key = SortedTriangleKey(tri);
                    if (corridorKeys.Contains(key))
                    {
                        throw new InvalidOperationException(
                            Delaunay2DError.Format(
                                Delaunay2DErrorCategory.Corridor,
                                $"Corridor triangle was not removed cleanly for constraint edge ({a},{b}): triangle ({tri.A},{tri.B},{tri.C})."));
                    }
                }

                AssertNoDuplicateTriangles(working, $"after enforcing constraint ({a},{b})");
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
                throw new InvalidOperationException(
                    Delaunay2DError.Format(
                        Delaunay2DErrorCategory.Corridor,
                        $"Constraint corridor is not a single connected component for edge ({a},{b})."));
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

        private static List<int> BuildBoundaryRingOrThrow(int a, int b, Dictionary<int, List<int>> adjacency, IReadOnlyList<RealPoint2D> points)
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

            if (Geometry2DIntersections.HasSelfIntersectionProper(ring, points))
            {
                throw new InvalidOperationException($"Constraint corridor boundary self-intersects for edge ({a},{b}).");
            }

            return ring;
        }

        private static (int, int, int) SortedTriangleKey(Triangle2D tri)
        {
            int a = tri.A;
            int b = tri.B;
            int c = tri.C;
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);
            return (a, b, c);
        }

        private static void AssertNoDuplicateTriangles(
            IReadOnlyList<Triangle2D> triangles,
            string context)
        {
            var seen = new HashSet<(int, int, int)>();
            foreach (var tri in triangles)
            {
                var key = SortedTriangleKey(tri);
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate triangle detected in {context}: ({tri.A},{tri.B},{tri.C}) with key ({key.Item1},{key.Item2},{key.Item3}).");
                }
            }
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

            throw new InvalidOperationException(
                Delaunay2DError.Format(
                    Delaunay2DErrorCategory.Corridor,
                    $"Constrained edge ({a},{b}) is missing after corridor re-triangulation."));
        }

        internal static void DefaultCorridorDumpHandler(CorridorDump dump)
        {
            Console.WriteLine($"[CDT] {dump.Context} dump for AB = ({dump.AB.A},{dump.AB.B})");
            Console.WriteLine("Outer ring: " + string.Join(",", dump.OuterRing));
            if (dump.InnerRings.Count > 0)
            {
                Console.WriteLine($"Inner rings: {dump.InnerRings.Count}");
                for (int i = 0; i < dump.InnerRings.Count; i++)
                {
                    Console.WriteLine($"  inner[{i}] = {string.Join(",", dump.InnerRings[i])}");
                }
            }

            Console.WriteLine("Corridor triangles: " + string.Join(",", dump.CorridorTriangles));
        }
    }
}

