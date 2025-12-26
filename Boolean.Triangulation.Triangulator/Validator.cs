using System;
using System.Collections.Generic;
using Geometry;

namespace ConstrainedTriangulator
{
    internal static class Validator
    {
        public static void ValidateFullTriangulation(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments,
            IReadOnlyList<(int A, int B, int C)> triangles)
        {
            int vertexCount = points.Count;
            if (vertexCount < 3)
                throw new InvalidOperationException("Not enough vertices.");

            // --- 1) Basic triangle sanity and edge usage counts ---
            var edgeUse = new Dictionary<(int, int), int>();

            void AddEdgeUse(int a, int b)
            {
                if (a == b)
                    throw new InvalidOperationException("Triangle has duplicate vertices.");

                var key = NormalizeEdge(a, b);
                if (!edgeUse.TryGetValue(key, out int cnt))
                    edgeUse[key] = 1;
                else
                    edgeUse[key] = cnt + 1;
            }

            foreach (var tri in triangles)
            {
                int a = tri.A;
                int b = tri.B;
                int c = tri.C;

                if (a < 0 || a >= vertexCount ||
                    b < 0 || b >= vertexCount ||
                    c < 0 || c >= vertexCount)
                {
                    throw new InvalidOperationException("Triangle has out-of-range vertex index.");
                }

                if (a == b || b == c || c == a)
                    throw new InvalidOperationException("Triangle has repeated vertices.");

                AddEdgeUse(a, b);
                AddEdgeUse(b, c);
                AddEdgeUse(c, a);
            }

            // --- 2) Build segment edge set ---
            var segmentEdges = new HashSet<(int, int)>();
            foreach (var seg in segments)
            {
                int a = seg.A;
                int b = seg.B;
                if (a < 0 || a >= vertexCount ||
                    b < 0 || b >= vertexCount)
                {
                    throw new InvalidOperationException("Segment has out-of-range vertex index.");
                }

                segmentEdges.Add(NormalizeEdge(a, b));
            }

            // --- 3) Union of all edges for Euler count ---
            var allEdges = new HashSet<(int, int)>(segmentEdges);
            foreach (var kv in edgeUse.Keys)
                allEdges.Add(kv);

            // --- 4) Edge manifoldness checks ---
            foreach (var edge in allEdges)
            {
                edgeUse.TryGetValue(edge, out int useCount);
                bool isSegment = segmentEdges.Contains(edge);

                if (useCount == 0)
                {
                    throw new InvalidOperationException(
                        $"Edge {edge} is not used by any triangle (gap in triangulation?).");
                }

                if (!isSegment)
                {
                    if (useCount != 2)
                    {
                        throw new InvalidOperationException(
                            $"Interior edge {edge} is used by {useCount} triangles (expected 2).");
                    }
                }
                else
                {
                    if (useCount < 1 || useCount > 2)
                    {
                        throw new InvalidOperationException(
                            $"Constraint edge {edge} is used by {useCount} triangles (expected 1 or 2).");
                    }
                }
            }

            // --- 5) Estimate number of constraint cycles (outer + holes) ---
            int cycleCount = CountConstraintComponents(vertexCount, segments);
            int holeCount = Math.Max(0, cycleCount - 1);

            // --- 6) Euler check: T = E - V + 1 - h ---
            int V = vertexCount;
            int E = allEdges.Count;
            int T = triangles.Count;
            int expectedT = E - V + 1 - holeCount;

            if (T != expectedT)
            {
                throw new InvalidOperationException(
                    $"Euler check failed: V={V}, E={E}, h={holeCount}, expected T={expectedT}, actual T={T}.");
            }
        }

        private static (int, int) NormalizeEdge(int a, int b)
        {
            return a < b ? (a, b) : (b, a);
        }

        private static int CountConstraintComponents(
            int vertexCount,
            IReadOnlyList<(int A, int B)> segments)
        {
            var adjacency = new List<int>[vertexCount];
            var hasConstraint = new bool[vertexCount];

            void AddAdj(int u, int v)
            {
                adjacency[u] ??= new List<int>();
                adjacency[u].Add(v);
                hasConstraint[u] = true;
            }

            foreach (var seg in segments)
            {
                AddAdj(seg.A, seg.B);
                AddAdj(seg.B, seg.A);
            }

            var visited = new bool[vertexCount];
            int components = 0;

            for (int v = 0; v < vertexCount; v++)
            {
                if (visited[v] || !hasConstraint[v])
                    continue;

                components++;
                var stack = new Stack<int>();
                stack.Push(v);
                visited[v] = true;

                while (stack.Count > 0)
                {
                    int cur = stack.Pop();
                    var nbrs = adjacency[cur];
                    if (nbrs == null) continue;

                    foreach (var w in nbrs)
                    {
                        if (!visited[w])
                        {
                            visited[w] = true;
                            stack.Push(w);
                        }
                    }
                }
            }

            return components;
        }
    }
}
