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
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)>? segments = null)
        {
            if (ring is null) throw new ArgumentNullException(nameof(ring));
            if (points is null) throw new ArgumentNullException(nameof(points));
            var poly = ValidateAndNormalizeRing(ring, points, allowDuplicateVertices: false);

            return EarClip(poly, points, segments);
        }

        internal static List<Triangle2D> TriangulateWithHoles(
            int[] outerRing,
            IReadOnlyList<int[]> innerRings,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)>? segments = null)
        {
            if (outerRing is null) throw new ArgumentNullException(nameof(outerRing));
            if (innerRings is null) throw new ArgumentNullException(nameof(innerRings));
            if (points is null) throw new ArgumentNullException(nameof(points));

            if (innerRings.Count == 0)
            {
                return TriangulateSimpleRing(outerRing, points, segments);
            }

            // Normalize orientations: outer CCW, holes CW.
            if (SignedArea(points, outerRing) < 0)
            {
                Array.Reverse(outerRing);
            }

            var normalizedHoles = new List<int[]>(innerRings.Count);
            foreach (var hole in innerRings)
            {
                var copy = new int[hole.Length];
                Array.Copy(hole, copy, hole.Length);
                if (SignedArea(points, copy) > 0)
                {
                    Array.Reverse(copy);
                }
                normalizedHoles.Add(copy);
            }

            // Preconditions: outer CCW, holes CW, disjoint and inside outer.
            var merged = BuildBridgedRing(outerRing, normalizedHoles, points);
            // Allow bridge duplicates when validating.
            var poly = ValidateAndNormalizeRing(merged, points, allowDuplicateVertices: true);
            return EarClip(poly, points, segments);
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
            IReadOnlyList<RealPoint2D> points,
            bool allowDuplicateVertices = false)
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

                if (!allowDuplicateVertices && !seen.Add(idx))
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

            if (!allowDuplicateVertices)
            {
                ValidateRingIsSimple(workingRing, points);
            }
            return workingRing;
        }

        private static void ValidateRingIsSimple(IReadOnlyList<int> ring, IReadOnlyList<RealPoint2D> points)
        {
            if (Geometry2DIntersections.HasSelfIntersectionProper(ring, points))
            {
                throw new InvalidOperationException("TriangulateSimpleRing: polygon ring is self-intersecting and cannot be triangulated.");
            }
        }

        private static int[] BuildBridgedRing(
            int[] outerRing,
            IReadOnlyList<int[]> innerRings,
            IReadOnlyList<RealPoint2D> points)
        {
            var currentOuter = new List<int>(outerRing);
            foreach (var hole in innerRings)
            {
                currentOuter = BridgeSingleHole(currentOuter, hole, points);
            }

            return currentOuter.ToArray();
        }

        private static List<int> BridgeSingleHole(
            List<int> outerRing,
            int[] holeRing,
            IReadOnlyList<RealPoint2D> points)
        {
            if (holeRing.Length < 3)
            {
                throw new InvalidOperationException("Hole ring must have at least 3 vertices.");
            }

            int holeBridgeIndex = 0;
            double maxX = points[holeRing[0]].X;
            for (int i = 1; i < holeRing.Length; i++)
            {
                var v = points[holeRing[i]];
                if (v.X > maxX || (Math.Abs(v.X - maxX) < 1e-12 && v.Y < points[holeRing[holeBridgeIndex]].Y))
                {
                    maxX = v.X;
                    holeBridgeIndex = i;
                }
            }

            int holeV = holeRing[holeBridgeIndex];
            int outerV = FindBridgeOuterVertex(outerRing, holeRing, holeV, points);

            var newOuter = new List<int>(outerRing.Count + holeRing.Length + 2);
            int outerPos = outerRing.IndexOf(outerV);
            if (outerPos < 0)
            {
                throw new InvalidOperationException("Bridge outer vertex not found in outer ring.");
            }

            for (int i = 0; i <= outerPos; i++)
            {
                newOuter.Add(outerRing[i]);
            }

            newOuter.Add(holeV);

            for (int i = 1; i < holeRing.Length; i++)
            {
                int idx = (holeBridgeIndex + i) % holeRing.Length;
                newOuter.Add(holeRing[idx]);
            }

            newOuter.Add(outerV);

            for (int i = outerPos + 1; i < outerRing.Count; i++)
            {
                newOuter.Add(outerRing[i]);
            }

            return newOuter;
        }

        private static int FindBridgeOuterVertex(
            List<int> outerRing,
            int[] holeRing,
            int holeV,
            IReadOnlyList<RealPoint2D> points)
        {
            var ph = points[holeV];
            double bestDx = double.NegativeInfinity;
            int bestOuter = -1;

            foreach (var ov in outerRing)
            {
                var po = points[ov];
                double dx = po.X - ph.X;
                if (dx <= 1e-12)
                {
                    continue;
                }

                if (!SegmentVisibleFrom(holeV, ov, outerRing, holeRing, points))
                {
                    continue;
                }

                if (dx > bestDx || (bestOuter < 0) || (Math.Abs(dx - bestDx) < 1e-12 && po.Y < points[bestOuter].Y))
                {
                    bestDx = dx;
                    bestOuter = ov;
                }
            }

            if (bestOuter < 0)
            {
                throw new InvalidOperationException("Failed to find a visible outer vertex to bridge the hole.");
            }

            return bestOuter;
        }

        private static bool SegmentVisibleFrom(
            int a,
            int b,
            List<int> outerRing,
            int[] holeRing,
            IReadOnlyList<RealPoint2D> points)
        {
            var pa = points[a];
            var pb = points[b];

            if (IntersectsRingProperly(a, b, pa, pb, outerRing, points))
            {
                return false;
            }

            if (IntersectsRingProperly(a, b, pa, pb, holeRing, points))
            {
                return false;
            }

            return true;
        }

        private static bool IntersectsRingProperly(
            int a,
            int b,
            RealPoint2D pa,
            RealPoint2D pb,
            IReadOnlyList<int> ring,
            IReadOnlyList<RealPoint2D> points)
        {
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                int u = ring[i];
                int v = ring[(i + 1) % n];

                if (u == a || v == a || u == b || v == b)
                {
                    continue;
                }

                var pu = points[u];
                var pv = points[v];
                if (Geometry2DIntersections.SegmentsIntersectProper(in pa, in pb, in pu, in pv))
                {
                    return true;
                }
            }

            return false;
        }

        private static double SignedArea(IReadOnlyList<RealPoint2D> points, int[] ring)
        {
            double area2 = 0.0;
            for (int i = 0; i < ring.Length; i++)
            {
                var p0 = points[ring[i]];
                var p1 = points[ring[(i + 1) % ring.Length]];
                area2 += p0.X * p1.Y - p1.X * p0.Y;
            }
            return 0.5 * area2;
        }

        private static double SignedArea(IReadOnlyList<RealPoint2D> points, IReadOnlyList<int> ring)
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

        private static List<Triangle2D> EarClip(
            List<int> poly,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)>? segments)
        {
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

                    if (segments != null)
                    {
                        if (!IsEdgeAllowed(prevIndex, currIndex, segments) ||
                            !IsEdgeAllowed(currIndex, nextIndex, segments) ||
                            !IsEdgeAllowed(nextIndex, prevIndex, segments))
                        {
                            continue;
                        }
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

            if (segments != null &&
                (!IsEdgeAllowed(poly[0], poly[1], segments) ||
                 !IsEdgeAllowed(poly[1], poly[2], segments) ||
                 !IsEdgeAllowed(poly[2], poly[0], segments)))
            {
                throw new InvalidOperationException("Ear clipping failed: final triangle violates constrained edge rules.");
            }

            triangles.Add(new Triangle2D(poly[0], poly[1], poly[2], points));
            return triangles;
        }

        private static bool IsEdgeAllowed(int u, int v, IReadOnlyList<(int A, int B)> segments)
        {
            if (EdgeExistsInSegments(u, v, segments))
            {
                return true;
            }

            return CanCreateEdge(u, v, segments);
        }

        private static bool EdgeExistsInSegments(int u, int v, IReadOnlyList<(int A, int B)> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var (a, b) = segments[i];
                if ((a == u && b == v) || (a == v && b == u))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanCreateEdge(
            int u,
            int v,
            IReadOnlyList<(int A, int B)> segments)
        {
            if (u == v)
                return false;

            var neighborsU = new HashSet<int>();
            var neighborsV = new HashSet<int>();

            for (int i = 0; i < segments.Count; i++)
            {
                var (a, b) = segments[i];

                if (a == u) neighborsU.Add(b);
                else if (b == u) neighborsU.Add(a);

                if (a == v) neighborsV.Add(b);
                else if (b == v) neighborsV.Add(a);
            }

            if (neighborsU.Count == 0 || neighborsV.Count == 0)
                return true;

            if (neighborsU.Count > neighborsV.Count)
            {
                (neighborsU, neighborsV) = (neighborsV, neighborsU);
            }

            foreach (var n in neighborsU)
            {
                if (neighborsV.Contains(n))
                    return true;
            }

            return false;
        }
    }
}
