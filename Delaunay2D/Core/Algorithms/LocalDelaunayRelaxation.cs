using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal static class LocalDelaunayRelaxation
    {
        internal static void Relax(
            IReadOnlyList<RealPoint2D> points,
            List<Triangle2D> triangles,
            IReadOnlyList<int> patchTriangleIndices,
            HashSet<Edge2D> constrainedEdges,
            bool log = false)
        {
            var patchSet = new HashSet<int>(patchTriangleIndices);
            var queue = new Queue<Edge2D>();
            var seenEdges = new HashSet<Edge2D>();

            foreach (var e in CollectPatchEdges(triangles, patchTriangleIndices))
            {
                if (constrainedEdges.Contains(e))
                {
                    continue;
                }
                if (seenEdges.Add(e))
                {
                    queue.Enqueue(e);
                }
            }

            while (queue.Count > 0)
            {
                var edge = queue.Dequeue();
                var incident = FindIncidentPatchTriangles(triangles, edge, patchSet);
                if (incident.Count != 2)
                {
                    continue;
                }

                int t0 = incident[0];
                int t1 = incident[1];
                var tri0 = triangles[t0];
                var tri1 = triangles[t1];

                int a = edge.A;
                int b = edge.B;
                int c = OppositeVertex(tri0, a, b);
                int d = OppositeVertex(tri1, a, b);

                // Constrained edge cannot be flipped.
                if (constrainedEdges.Contains(edge))
                {
                    if (log) Console.WriteLine($"[Relax] Skip {a}-{b}: constrained.");
                    continue;
                }

                var pa = points[a];
                var pb = points[b];
                var pc = points[c];
                var pd = points[d];

                double incircle = Geometry2DPredicates.CircumcircleValue(in pd, in pa, in pb, in pc);
                if (incircle >= -Geometry2DPredicates.Epsilon)
                {
                    if (log) Console.WriteLine($"[Relax] Keep {a}-{b}: already Delaunay (value={incircle}).");
                    continue;
                }

                // Candidate flipped triangles: (c,d,a) and (d,c,b)
                Triangle2D flipped0;
                Triangle2D flipped1;
                try
                {
                    flipped0 = new Triangle2D(c, d, a, points);
                    flipped1 = new Triangle2D(d, c, b, points);
                }
                catch
                {
                    if (log) Console.WriteLine($"[Relax] Skip {a}-{b}: flip would create degenerate triangle.");
                    continue;
                }

                triangles[t0] = flipped0;
                triangles[t1] = flipped1;
                if (log) Console.WriteLine($"[Relax] Flipped {a}-{b} -> {c}-{d}.");

                // Queue new interior edges of the flipped configuration.
                var candidates = new[]
                {
                    new Edge2D(c, d),
                    new Edge2D(c, a),
                    new Edge2D(d, a),
                    new Edge2D(d, b),
                    new Edge2D(c, b)
                };

                foreach (var cand in candidates)
                {
                    if (constrainedEdges.Contains(cand))
                    {
                        continue;
                    }

                    var inc = FindIncidentPatchTriangles(triangles, cand, patchSet);
                    if (inc.Count == 2 && seenEdges.Add(cand))
                    {
                        queue.Enqueue(cand);
                    }
                }
            }
        }

        private static List<Edge2D> CollectPatchEdges(List<Triangle2D> triangles, IReadOnlyList<int> patchTriangleIndices)
        {
            var edges = new List<Edge2D>();
            foreach (var idx in patchTriangleIndices)
            {
                var tri = triangles[idx];
                edges.Add(new Edge2D(tri.A, tri.B));
                edges.Add(new Edge2D(tri.B, tri.C));
                edges.Add(new Edge2D(tri.C, tri.A));
            }

            return edges;
        }

        private static List<int> FindIncidentPatchTriangles(
            List<Triangle2D> triangles,
            Edge2D edge,
            HashSet<int> patchSet)
        {
            var list = new List<int>(2);
            foreach (var idx in patchSet)
            {
                var tri = triangles[idx];
                if (Geometry2DIntersections.TriangleHasUndirectedEdge(tri, edge.A, edge.B))
                {
                    list.Add(idx);
                    if (list.Count == 2) break;
                }
            }

            return list;
        }

        private static int OppositeVertex(Triangle2D tri, int a, int b)
        {
            if (tri.A != a && tri.A != b) return tri.A;
            if (tri.B != a && tri.B != b) return tri.B;
            return tri.C;
        }
    }
}
