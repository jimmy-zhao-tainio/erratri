using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;

namespace Delaunay2D
{
    internal static class BowyerWatsonTriangulator
    {
        internal static List<Triangle2D> Triangulate(IReadOnlyList<RealPoint2D> points)
        {
            if (points is null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count < 3)
            {
                throw new ArgumentException("Need at least 3 points for triangulation.", nameof(points));
            }

            var allPoints = new List<RealPoint2D>(points);

            var superTriangle = new SuperTriangle(points);
            var initialTriangle = superTriangle.AppendTo(allPoints);

            var triangles = new List<Triangle2D> { initialTriangle };

            // Incremental Bowyer-Watson (O(n^2) point location).
            for (int i = 0; i < points.Count; i++)
            {
                var point = allPoints[i];
                var boundary = new Dictionary<(int, int), (int A, int B)>();
                var updatedTriangles = new List<Triangle2D>();
                var edgeToTriangles = new Dictionary<Edge2D, List<int>>();

                for (int t = 0; t < triangles.Count; t++)
                {
                    var tri = triangles[t];
                    AddTriangleEdge(edgeToTriangles, new Edge2D(tri.A, tri.B), t);
                    AddTriangleEdge(edgeToTriangles, new Edge2D(tri.B, tri.C), t);
                    AddTriangleEdge(edgeToTriangles, new Edge2D(tri.C, tri.A), t);
                }

                var candidateBad = new HashSet<int>();
                for (int t = 0; t < triangles.Count; t++)
                {
                    var triangle = triangles[t];
                    var a = allPoints[triangle.A];
                    var b = allPoints[triangle.B];
                    var c = allPoints[triangle.C];

                    double incircle = Geometry2DPredicates.CircumcircleValue(in point, in a, in b, in c);
                    if (incircle < -Geometry2DPredicates.Epsilon)
                    {
                        candidateBad.Add(t);
                    }
                }

                if (candidateBad.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Bowyer-Watson failed to find a cavity for point index {i}. " +
                        "This indicates an inconsistency in circumcircle tests or degeneracy handling.");
                }

                var badTriangles = new HashSet<int>();
                var stack = new Stack<int>();
                stack.Push(candidateBad.First());
                while (stack.Count > 0)
                {
                    int t = stack.Pop();
                    if (!candidateBad.Contains(t) || !badTriangles.Add(t))
                    {
                        continue;
                    }

                    var tri = triangles[t];
                    var edges = new[]
                    {
                        new Edge2D(tri.A, tri.B),
                        new Edge2D(tri.B, tri.C),
                        new Edge2D(tri.C, tri.A)
                    };

                    foreach (var edge in edges)
                    {
                        if (!edgeToTriangles.TryGetValue(edge, out var adj))
                        {
                            continue;
                        }

                        foreach (var neighbor in adj)
                        {
                            if (!badTriangles.Contains(neighbor) && candidateBad.Contains(neighbor))
                            {
                                stack.Push(neighbor);
                            }
                        }
                    }
                }

                foreach (var t in badTriangles)
                {
                    var tri = triangles[t];
                    AddOrToggleBoundaryEdge(boundary, tri.A, tri.B);
                    AddOrToggleBoundaryEdge(boundary, tri.B, tri.C);
                    AddOrToggleBoundaryEdge(boundary, tri.C, tri.A);
                }

                for (int t = 0; t < triangles.Count; t++)
                {
                    if (badTriangles.Contains(t))
                    {
                        continue;
                    }

                    updatedTriangles.Add(triangles[t]);
                }

                var edgeUsage = new Dictionary<Edge2D, int>();
                foreach (var tri in updatedTriangles)
                {
                    AccumulateEdge(edgeUsage, new Edge2D(tri.A, tri.B));
                    AccumulateEdge(edgeUsage, new Edge2D(tri.B, tri.C));
                    AccumulateEdge(edgeUsage, new Edge2D(tri.C, tri.A));
                }

                foreach (var kvp in boundary.Values)
                {
                    var edge = new Edge2D(kvp.A, kvp.B);
                    if (edgeUsage.TryGetValue(edge, out int count) && count >= 2)
                    {
                        continue;
                    }

                    var newTriangle = new Triangle2D(i, kvp.A, kvp.B, allPoints);
                    updatedTriangles.Add(newTriangle);
                    AccumulateEdge(edgeUsage, new Edge2D(newTriangle.A, newTriangle.B));
                    AccumulateEdge(edgeUsage, new Edge2D(newTriangle.B, newTriangle.C));
                    AccumulateEdge(edgeUsage, new Edge2D(newTriangle.C, newTriangle.A));
                }

                triangles = updatedTriangles;
            }

            ValidateTriangulation(allPoints, triangles);
            return triangles;
        }

        private static void AccumulateEdge(Dictionary<Edge2D, int> counts, Edge2D edge)
        {
            if (counts.TryGetValue(edge, out int existing))
            {
                counts[edge] = existing + 1;
            }
            else
            {
                counts[edge] = 1;
            }
        }

        private static void AddOrToggleBoundaryEdge(
            Dictionary<(int, int), (int A, int B)> boundary,
            int a,
            int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (boundary.ContainsKey(key))
            {
                boundary.Remove(key);
            }
            else
            {
                boundary[key] = (a, b);
            }
        }

        private static void ValidateTriangulation(IReadOnlyList<RealPoint2D> points, IReadOnlyList<Triangle2D> triangles)
        {
            var edgeCounts = new Dictionary<Edge2D, int>();
            var edgeTriangles = new Dictionary<Edge2D, List<(int A, int B, int C)>>();

            foreach (var triangle in triangles)
            {
                var pa = points[triangle.A];
                var pb = points[triangle.B];
                var pc = points[triangle.C];

                var orient = Geometry2DPredicates.Orientation(in pa, in pb, in pc);
                if (orient != OrientationKind.CounterClockwise)
                {
                    throw new InvalidOperationException("Triangulation produced a non-CCW triangle.");
                }

                AccumulateEdge(edgeCounts, new Edge2D(triangle.A, triangle.B));
                AccumulateEdge(edgeCounts, new Edge2D(triangle.B, triangle.C));
                AccumulateEdge(edgeCounts, new Edge2D(triangle.C, triangle.A));

                TrackEdgeTriangle(edgeTriangles, new Edge2D(triangle.A, triangle.B), triangle);
                TrackEdgeTriangle(edgeTriangles, new Edge2D(triangle.B, triangle.C), triangle);
                TrackEdgeTriangle(edgeTriangles, new Edge2D(triangle.C, triangle.A), triangle);
            }

            foreach (var kvp in edgeCounts)
            {
                if (kvp.Value > 2)
                {
                    var tris = edgeTriangles[kvp.Key];
                    throw new InvalidOperationException(
                        $"Edge manifoldness violated: edge ({kvp.Key.A},{kvp.Key.B}) used {kvp.Value} times; triangles: " +
                        string.Join("; ", tris.ConvertAll(t => $"({t.A},{t.B},{t.C})")));
                }
            }
        }

        private static void AddTriangleEdge(Dictionary<Edge2D, List<int>> edgeToTriangles, Edge2D edge, int triangleIndex)
        {
            if (!edgeToTriangles.TryGetValue(edge, out var list))
            {
                list = new List<int>();
                edgeToTriangles[edge] = list;
            }

            list.Add(triangleIndex);
        }

        private static void TrackEdgeTriangle(
            Dictionary<Edge2D, List<(int A, int B, int C)>> map,
            Edge2D edge,
            Triangle2D triangle)
        {
            if (!map.TryGetValue(edge, out var list))
            {
                list = new List<(int A, int B, int C)>();
                map[edge] = list;
            }

            list.Add((triangle.A, triangle.B, triangle.C));
        }
    }
}
