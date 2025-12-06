using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal static class PolygonTriangulator2D
    {
        // Epsilon policy: all degeneracy/orientation checks in this triangulator use Geometry2DPredicates.Epsilon.
        // A polygon with |area| <= epsilon is treated as too small/degenerate to triangulate reliably.
        // Ear acceptance also uses this epsilon to filter near-collinear ears.

        /// <summary>
        /// Triangulate a single simple polygon ring given as a sequence of vertex indices.
        /// The ring may optionally be closed; a repeated last index is ignored. All indices must be valid.
        /// Invariants:
        /// - Ring is simple at the index level (no duplicates except an optional closure).
        /// - Returned triangles cover the polygon; each Triangle2D is CCW and non-degenerate.
        /// Throws InvalidOperationException on invalid or non-triangulable input.
        /// </summary>
        internal static List<Triangle2D> TriangulateSimpleRing(
            IReadOnlyList<int> ring,
            IReadOnlyList<RealPoint2D> points)
        {
            if (ring is null) throw new ArgumentNullException(nameof(ring));
            if (points is null) throw new ArgumentNullException(nameof(points));
            var poly = ValidateAndNormalizeRing(ring, points);

            var triangles = new List<Triangle2D>(poly.Count - 2);

            while (poly.Count > 3)
            {
                bool earFound = false;
                int n = poly.Count;

                for (int i = 0; i < n; i++)
                {
                    int prevIndex = poly[(i - 1 + n) % n];
                    int currIndex = poly[i];
                    int nextIndex = poly[(i + 1) % n];

                    var pPrev = points[prevIndex];
                    var pCurr = points[currIndex];
                    var pNext = points[nextIndex];

                    var orientation = Geometry2DPredicates.Orientation(in pPrev, in pCurr, in pNext);
                    if (orientation != OrientationKind.CounterClockwise)
                    {
                        continue;
                    }

                    double earArea = 0.5 * ((pCurr.X - pPrev.X) * (pNext.Y - pPrev.Y) - (pCurr.Y - pPrev.Y) * (pNext.X - pPrev.X));
                    if (Math.Abs(earArea) <= Geometry2DPredicates.Epsilon)
                    {
                        continue; // near-collinear ear, skip
                    }

                    bool anyInside = false;
                    for (int k = 0; k < n; k++)
                    {
                        int candidateIndex = poly[k];
                        if (candidateIndex == prevIndex || candidateIndex == currIndex || candidateIndex == nextIndex)
                        {
                            continue;
                        }

                        var candidatePoint = points[candidateIndex];
                        if (IsPointInTriangleStrict(in candidatePoint, in pPrev, in pCurr, in pNext))
                        {
                            anyInside = true;
                            break;
                        }
                    }

                    if (anyInside)
                    {
                        continue;
                    }

                    triangles.Add(new Triangle2D(prevIndex, currIndex, nextIndex, points));
                    poly.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    throw new InvalidOperationException(
                        "Ear clipping failed: no valid ear found for a non-degenerate, simple polygon. " +
                        "This usually indicates severe numeric degeneracy (e.g., nearly collinear vertices) relative to the chosen epsilon.");
                }
            }

            triangles.Add(new Triangle2D(poly[0], poly[1], poly[2], points));
            return triangles;
        }

        private static bool IsPointInTriangleStrict(
            in RealPoint2D p,
            in RealPoint2D a,
            in RealPoint2D b,
            in RealPoint2D c)
        {
            var w0 = Geometry2DPredicates.Orientation(in a, in b, in p);
            var w1 = Geometry2DPredicates.Orientation(in b, in c, in p);
            var w2 = Geometry2DPredicates.Orientation(in c, in a, in p);

            return w0 == OrientationKind.CounterClockwise &&
                   w1 == OrientationKind.CounterClockwise &&
                   w2 == OrientationKind.CounterClockwise;
        }

        private static List<int> ValidateAndNormalizeRing(
            IReadOnlyList<int> ring,
            IReadOnlyList<RealPoint2D> points)
        {
            if (ring.Count < 3)
            {
                throw new ArgumentException("TriangulateSimpleRing requires at least 3 vertices.", nameof(ring));
            }

            var workingRing = new List<int>(ring.Count);
            bool hasClosure = ring.Count >= 2 && ring[0] == ring[ring.Count - 1];
            int limit = hasClosure ? ring.Count - 1 : ring.Count;
            for (int i = 0; i < limit; i++)
            {
                workingRing.Add(ring[i]);
            }

            if (workingRing.Count < 3)
            {
                throw new ArgumentException("TriangulateSimpleRing requires at least 3 vertices after removing closure.", nameof(ring));
            }

            var seen = new HashSet<int>();
            for (int i = 0; i < workingRing.Count; i++)
            {
                int idx = workingRing[i];
                if (idx < 0 || idx >= points.Count)
                {
                    throw new ArgumentException("TriangulateSimpleRing encountered a vertex index out of range.", nameof(ring));
                }

                if (!seen.Add(idx))
                {
                    throw new InvalidOperationException($"TriangulateSimpleRing requires a simple polygon: vertex index {idx} appears more than once.");
                }
            }

            double area = 0.0;
            for (int i = 0; i < workingRing.Count; i++)
            {
                var p0 = points[workingRing[i]];
                var p1 = points[workingRing[(i + 1) % workingRing.Count]];
                area += p0.X * p1.Y - p1.X * p0.Y;
            }
            area *= 0.5;
            if (Math.Abs(area) <= Geometry2DPredicates.Epsilon)
            {
                throw new InvalidOperationException("Polygon area is too small or degenerate for triangulation.");
            }
            if (area < 0)
            {
                workingRing.Reverse();
            }

            ValidateRingIsSimple(workingRing, points);
            return workingRing;
        }

        private static void ValidateRingIsSimple(IReadOnlyList<int> ring, IReadOnlyList<RealPoint2D> points)
        {
            if (Geometry2DIntersections.HasSelfIntersectionProper(ring, points))
            {
                throw new InvalidOperationException("TriangulateSimpleRing: polygon ring is self-intersecting and cannot be triangulated.");
            }
        }
    }
}
