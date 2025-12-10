using System;
using System.Collections.Generic;
using System.Linq;
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
        ///     * ProperInterior / EndpointInside â†’ always enter the corridor.
        ///     * CollinearOverlap â†’ joins the corridor only if there is at least one ProperInterior/EndpointInside hit.
        ///     * TouchVertex / TouchEdge â†’ never enter the corridor; touch-only configurations do not build a corridor.
        /// - Pure collinear chains (no interior hits, at least one collinear overlap) throw with an explicit â€œcollinear chains are not yet supportedâ€ message.
        /// Geometry2DPredicates.Epsilon is used inside the classifier to separate â€œoverlapâ€ vs â€œtouchingâ€ for collinear and intersection tests; EnforceSegments itself performs only index-exact edge checks.
        /// </summary>
        internal static List<Triangle2D> EnforceSegments(
            IReadOnlyList<RealPoint2D> points,
            List<Triangle2D> triangles,
            IReadOnlyList<(int A, int B)> segments,
            Delaunay2DDebugOptions? debug = null,
            ConstraintEnforcer2DOptions? options = null)
        {
            if (segments == null || segments.Count == 0)
            {
                return triangles;
            }
            ValidateConstraints(points, segments);

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

                var meshTopology = new ListBackedMeshTopology2D(working);
                IReadOnlyList<Triangle2D>? meshBeforeDump = null;
                if (debug?.EnableCorridorDump == true)
                {
                    meshBeforeDump = meshTopology.Triangles.ToArray();
                }

                var (boundary, corridorTriangles) = CorridorBuilder.BuildCorridor(meshTopology, points, edge);

                if (boundary.InnerRings.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Corridor for constrained edge ({edge.A},{edge.B}) contains internal loops (holes) which are not yet supported.");
                }

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
                    newTriangles = PolygonTriangulator2D.TriangulateSimpleRing(ring, points, segments);
                }
                else
                {
                    newTriangles = PolygonTriangulator2D.TriangulateWithHoles(boundary.OuterRing, boundary.InnerRings, points, segments);
                }
                if (boundary.InnerRings.Count == 0)
                {
                    int expected = ring.Count - 2;
                    if (newTriangles.Count != expected)
                    {
                        throw new InvalidOperationException(
                            $"PolygonTriangulator2D returned {newTriangles.Count} triangles for corridor ring with {ring.Count} vertices " +
                            $"for constraint edge ({a},{b}); expected {expected}.");
                    }
                }
                foreach (var tri in newTriangles)
                {
                    if (!TriangleRespectsAngularAdjacency(tri.A, tri.B, tri.C, points, segments))
                    {
                        throw new InvalidOperationException(
                            $"Corridor triangulation produced triangle ({tri.A},{tri.B},{tri.C}) that violates local angular adjacency of constraints.");
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
                    LocalDelaunayRelaxation.Relax(points, working, patchIndices, frozen, log: debug?.EnableCorridorDump == true);
                }

                if (debug?.EnableCorridorDump == true)
                {
                    var meshAfter = working.ToArray();
                    var dump = new CorridorDump(
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
                            $"Corridor triangle was not removed cleanly for constraint edge ({a},{b}): triangle ({tri.A},{tri.B},{tri.C}).");
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

        private static void ValidateConstraints(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            // 1. Detect proper crossings between distinct constraints (excluding shared endpoints).
            for (int i = 0; i < segments.Count; i++)
            {
                var (a1, b1) = segments[i];
                var p1a = points[a1];
                var p1b = points[b1];
                for (int j = i + 1; j < segments.Count; j++)
                {
                    var (a2, b2) = segments[j];
                    if (a1 == a2 || a1 == b2 || b1 == a2 || b1 == b2)
                    {
                        continue; // shared endpoint is allowed
                    }

                    var p2a = points[a2];
                    var p2b = points[b2];
                    if (Geometry2DIntersections.SegmentsIntersectProper(in p1a, in p1b, in p2a, in p2b))
                    {
                        throw new InvalidOperationException("Crossing constraints are not supported (degree-2 boundary).");
                    }
                }
            }

            // 2. Pure collinear chains are unsupported.
            if (segments.Count == 0)
            {
                return;
            }
            // 2. "Pure collinear chain only" – narrow, constraint-level check.
            if (IsPureCollinearChain(points, segments))
            {
                throw new InvalidOperationException("Pure collinear constraint chains are not supported.");
            }
            /*
            var first = segments[0];
            var p0 = points[first.A];
            var p1 = points[first.B];
            double vx = p1.X - p0.X;
            double vy = p1.Y - p0.Y;

            bool allCollinear = true;
            for (int i = 1; i < segments.Count && allCollinear; i++)
            {
                var (a, b) = segments[i];
                var pa = points[a];
                var pb = points[b];

                double crossA = vx * (pa.Y - p0.Y) - vy * (pa.X - p0.X);
                double crossB = vx * (pb.Y - p0.Y) - vy * (pb.X - p0.X);

                if (Math.Abs(crossA) > Geometry2DPredicates.Epsilon ||
                    Math.Abs(crossB) > Geometry2DPredicates.Epsilon)
                {
                    allCollinear = false;
                }
            }

            if (allCollinear)
            {
                throw new InvalidOperationException("Pure collinear constraint chains are not supported.");
            }*/
        }

        private static bool AreAllPointsCollinear(IReadOnlyList<RealPoint2D> points)
        {
            if (points.Count <= 2)
                return true;

            // Find two distinct points.
            int i0 = 0;
            int i1 = 1;
            while (i1 < points.Count &&
                Math.Abs(points[i1].X - points[i0].X) <= Geometry2DPredicates.Epsilon &&
                Math.Abs(points[i1].Y - points[i0].Y) <= Geometry2DPredicates.Epsilon)
            {
                i1++;
            }

            if (i1 == points.Count)
                return true; // all identical

            var p0 = points[i0];
            var p1 = points[i1];
            double vx = p1.X - p0.X;
            double vy = p1.Y - p0.Y;

            for (int i = i1 + 1; i < points.Count; i++)
            {
                var p = points[i];
                double cross = vx * (p.Y - p0.Y) - vy * (p.X - p0.X);
                if (Math.Abs(cross) > Geometry2DPredicates.Epsilon)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPureCollinearChain(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            if (segments == null || segments.Count < 2)
                return false; // single segment is fine

            // Collect all vertices touched by constraints.
            var vertices = new HashSet<int>();
            foreach (var (a, b) in segments)
            {
                vertices.Add(a);
                vertices.Add(b);
            }

            if (vertices.Count < 3)
                return false; // two endpoints only, allowed

            // Pick two distinct vertices as a baseline.
            using var it = vertices.GetEnumerator();
            it.MoveNext();
            int i0 = it.Current;
            int i1 = i0;
            while (it.MoveNext())
            {
                i1 = it.Current;
                if (!AlmostSame(points[i0], points[i1]))
                    break;
            }

            if (i0 == i1 || AlmostSame(points[i0], points[i1]))
                return false; // degenerate, don't classify as chain

            var p0 = points[i0];
            var p1 = points[i1];
            double vx = p1.X - p0.X;
            double vy = p1.Y - p0.Y;

            // Check all constraint endpoints lie on the same line.
            foreach (var v in vertices)
            {
                var p = points[v];
                double cross = vx * (p.Y - p0.Y) - vy * (p.X - p0.X);
                if (Math.Abs(cross) > Geometry2DPredicates.Epsilon)
                {
                    return false; // not all collinear → not "collinear chain only"
                }
            }

            // Build degree map in the constraint graph.
            var degree = new Dictionary<int, int>();
            foreach (var (a, b) in segments)
            {
                if (!degree.TryGetValue(a, out var da)) da = 0;
                if (!degree.TryGetValue(b, out var db)) db = 0;
                degree[a] = da + 1;
                degree[b] = db + 1;
            }

            int deg1 = 0;
            foreach (var kvp in degree)
            {
                var d = kvp.Value;
                if (d > 2)
                    return false; // branching → not a simple chain
                if (d == 1)
                    deg1++;
            }

            // A simple open chain has exactly two degree-1 endpoints, all others degree-2.
            return deg1 == 2;
        }

        private static bool AlmostSame(RealPoint2D a, RealPoint2D b)
        {
            return Math.Abs(a.X - b.X) <= Geometry2DPredicates.Epsilon &&
                Math.Abs(a.Y - b.Y) <= Geometry2DPredicates.Epsilon;
        }

        private static bool TriangleRespectsAngularAdjacency(
            int a,
            int b,
            int c,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            return VertexHasAngularAdjacency(a, b, c, points, segments) &&
                   VertexHasAngularAdjacency(b, c, a, points, segments) &&
                   VertexHasAngularAdjacency(c, a, b, points, segments);
        }

        private static bool VertexHasAngularAdjacency(
            int v,
            int other1,
            int other2,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            var spokes = new List<(double angle, bool side1, bool side2)>();
            var p = points[v];

            static double AngleTo(RealPoint2D from, RealPoint2D to)
            {
                return Math.Atan2(to.Y - from.Y, to.X - from.X);
            }

            spokes.Add((AngleTo(p, points[other1]), true, false));
            spokes.Add((AngleTo(p, points[other2]), false, true));

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                int other = -1;
                if (seg.A == v) other = seg.B;
                else if (seg.B == v) other = seg.A;
                if (other == -1) continue;

                spokes.Add((AngleTo(p, points[other]), false, false));
            }

            spokes.Sort((x, y) => x.angle.CompareTo(y.angle));

            var merged = new List<(double angle, bool side1, bool side2)>();
            foreach (var s in spokes)
            {
                if (merged.Count == 0)
                {
                    merged.Add(s);
                    continue;
                }

                var last = merged[merged.Count - 1];
                if (AnglesCollinear(last.angle, s.angle))
                {
                    merged[merged.Count - 1] = (last.angle, last.side1 || s.side1, last.side2 || s.side2);
                }
                else
                {
                    merged.Add(s);
                }
            }

            if (merged.Count > 1 && AnglesCollinear(merged[0].angle, merged[merged.Count - 1].angle))
            {
                var first = merged[0];
                var last = merged[merged.Count - 1];
                merged[0] = (first.angle, first.side1 || last.side1, first.side2 || last.side2);
                merged.RemoveAt(merged.Count - 1);
            }

            int idx1 = -1;
            int idx2 = -1;
            for (int i = 0; i < merged.Count; i++)
            {
                var m = merged[i];
                if (m.side1) idx1 = i;
                if (m.side2) idx2 = i;
            }

            if (idx1 == -1 || idx2 == -1)
            {
                return false;
            }
            if (idx1 == idx2)
            {
                return true; // collinear sides; treated as adjacent
            }

            int n = merged.Count;
            return ((idx1 + 1) % n == idx2) || ((idx2 + 1) % n == idx1);
        }

        private static bool AnglesCollinear(double a, double b)
        {
            double diff = Math.Abs(a - b);
            if (diff > Math.PI)
            {
                diff = 2 * Math.PI - diff;
            }

            return diff <= 1e-12;
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

            throw new InvalidOperationException($"Constrained edge ({a},{b}) is missing after corridor re-triangulation.");
        }

        private static void DefaultCorridorDumpHandler(CorridorDump dump)
        {
            Console.WriteLine($"[CDT] Corridor dump for AB = ({dump.AB.A},{dump.AB.B})");
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
