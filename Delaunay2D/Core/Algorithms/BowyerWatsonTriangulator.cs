using System;
using System.Collections.Generic;
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
                var boundaryCounts = new Dictionary<Edge2D, int>();
                var updatedTriangles = new List<Triangle2D>();

                foreach (var triangle in triangles)
                {
                    var a = allPoints[triangle.A];
                    var b = allPoints[triangle.B];
                    var c = allPoints[triangle.C];

                    double incircle = Geometry2DPredicates.CircumcircleValue(in point, in a, in b, in c);
                    if (incircle < -Geometry2DPredicates.Epsilon)
                    {
                        AccumulateEdge(boundaryCounts, new Edge2D(triangle.A, triangle.B));
                        AccumulateEdge(boundaryCounts, new Edge2D(triangle.B, triangle.C));
                        AccumulateEdge(boundaryCounts, new Edge2D(triangle.C, triangle.A));
                        continue;
                    }

                    updatedTriangles.Add(triangle);
                }

                if (boundaryCounts.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Bowyer-Watson failed to find a cavity for point index {i}. " +
                        "This indicates an inconsistency in circumcircle tests or degeneracy handling.");
                }

                foreach (var kvp in boundaryCounts)
                {
                    if (kvp.Value != 1)
                    {
                        continue;
                    }

                    int a = kvp.Key.A;
                    int b = kvp.Key.B;
                    updatedTriangles.Add(new Triangle2D(i, a, b, allPoints));
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

        private static void ValidateTriangulation(IReadOnlyList<RealPoint2D> points, IReadOnlyList<Triangle2D> triangles)
        {
            var edgeCounts = new Dictionary<Edge2D, int>();

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
            }

            foreach (var kvp in edgeCounts)
            {
                if (kvp.Value > 2)
                {
                    throw new InvalidOperationException($"Edge manifoldness violated: edge ({kvp.Key.A},{kvp.Key.B}) used {kvp.Value} times.");
                }
            }
        }
    }
}
