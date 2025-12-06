using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    /// <summary>
    /// 2D constrained Delaunay triangulator over Geometry.RealPoint2D.
    /// This project is deliberately limited to Geometry primitives and System.*
    /// It must not depend on PSLG, Kernel or world-space mapping.
    /// </summary>
    public static class Delaunay2DTriangulator
    {
        /// <summary>
        /// Build a constrained Delaunay triangulation of the given points and segments
        /// in 2D. All segments must appear as edges in the triangulation, and no
        /// triangle edge may cross a constrained segment.
        /// 
        /// Coordinates are in a single 2D plane coordinate system; mapping to/from
        /// world space is handled by other phases, not here.
        /// </summary>
        public static Delaunay2DResult Run(in Delaunay2DInput input)
        {
            ValidateInput(input);

            int originalCount = input.Points.Count;
            var triangles = BowyerWatsonTriangulator.Triangulate(input.Points);
            triangles = FilterOutSuperTriangles(triangles, originalCount);

            if (input.Segments.Count > 0)
            {
                triangles = ConstraintEnforcer2D.EnforceSegments(input.Points, triangles, input.Segments);
            }

            var resultTriangles = new List<(int A, int B, int C)>();
            foreach (var triangle in triangles)
            {
                if (triangle.A >= originalCount || triangle.B >= originalCount || triangle.C >= originalCount)
                {
                    continue;
                }

                resultTriangles.Add((triangle.A, triangle.B, triangle.C));
            }

            return new Delaunay2DResult(input.Points, resultTriangles);
        }

        private static List<Triangle2D> FilterOutSuperTriangles(IReadOnlyList<Triangle2D> triangles, int originalCount)
        {
            var filtered = new List<Triangle2D>(triangles.Count);
            foreach (var triangle in triangles)
            {
                if (triangle.A < originalCount && triangle.B < originalCount && triangle.C < originalCount)
                {
                    filtered.Add(triangle);
                }
            }

            System.Diagnostics.Debug.Assert(AllIndicesInRange(filtered, originalCount));
            return filtered;
        }

        private static bool AllIndicesInRange(IEnumerable<Triangle2D> triangles, int originalCount)
        {
            foreach (var triangle in triangles)
            {
                if (triangle.A >= originalCount || triangle.B >= originalCount || triangle.C >= originalCount ||
                    triangle.A < 0 || triangle.B < 0 || triangle.C < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateInput(in Delaunay2DInput input)
        {
            if (input.Points is null)
            {
                throw new ArgumentNullException(nameof(input.Points));
            }

            int pointCount = input.Points.Count;
            if (pointCount < 3)
            {
                throw new ArgumentException("Need at least 3 points for triangulation.", nameof(input));
            }

            // Detect duplicates (O(n^2) acceptable for v1).
            for (int i = 0; i < pointCount; i++)
            {
                var pi = input.Points[i];
                for (int j = i + 1; j < pointCount; j++)
                {
                    var pj = input.Points[j];
                    double dist2 = Triangle2D.DistanceSquared(in pi, in pj);
                    if (dist2 <= Tolerances.EpsVertexSquared)
                    {
                        throw new ArgumentException("Delaunay2DInput contains duplicate or near-duplicate points.", nameof(input));
                    }
                }
            }

            // Detect all-collinear sets.
            var first = input.Points[0];
            var second = input.Points[1];
            bool allCollinear = true;
            for (int i = 2; i < pointCount; i++)
            {
                var current = input.Points[i];
                var orient = Geometry2DPredicates.Orientation(in first, in second, in current);
                if (orient != OrientationKind.Collinear)
                {
                    allCollinear = false;
                    break;
                }
            }

            if (allCollinear)
            {
                throw new ArgumentException("Cannot triangulate: all points are collinear within tolerance.", nameof(input));
            }

            return true;
        }
    }
}
