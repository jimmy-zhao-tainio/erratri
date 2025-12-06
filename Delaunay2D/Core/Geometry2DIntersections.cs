using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal static class Geometry2DIntersections
    {
        internal static bool SegmentsIntersectInclusive(
            in RealPoint2D p1,
            in RealPoint2D q1,
            in RealPoint2D p2,
            in RealPoint2D q2)
        {
            var o1 = Geometry2DPredicates.Orientation(in p1, in q1, in p2);
            var o2 = Geometry2DPredicates.Orientation(in p1, in q1, in q2);
            var o3 = Geometry2DPredicates.Orientation(in p2, in q2, in p1);
            var o4 = Geometry2DPredicates.Orientation(in p2, in q2, in q1);

            bool general = o1 != o2 && o3 != o4;
            if (general)
            {
                return true;
            }

            bool on1 = o1 == OrientationKind.Collinear && OnSegmentInclusive(in p1, in p2, in q1);
            bool on2 = o2 == OrientationKind.Collinear && OnSegmentInclusive(in p1, in q2, in q1);
            bool on3 = o3 == OrientationKind.Collinear && OnSegmentInclusive(in p2, in p1, in q2);
            bool on4 = o4 == OrientationKind.Collinear && OnSegmentInclusive(in p2, in q1, in q2);
            return on1 || on2 || on3 || on4;
        }

        internal static bool SegmentsIntersectProper(
            in RealPoint2D p1,
            in RealPoint2D q1,
            in RealPoint2D p2,
            in RealPoint2D q2)
        {
            var o1 = Geometry2DPredicates.Orientation(in p1, in q1, in p2);
            var o2 = Geometry2DPredicates.Orientation(in p1, in q1, in q2);
            var o3 = Geometry2DPredicates.Orientation(in p2, in q2, in p1);
            var o4 = Geometry2DPredicates.Orientation(in p2, in q2, in q1);

            bool general = o1 != o2 && o3 != o4;
            if (general)
            {
                return true;
            }

            bool allCollinear = o1 == OrientationKind.Collinear &&
                                o2 == OrientationKind.Collinear &&
                                o3 == OrientationKind.Collinear &&
                                o4 == OrientationKind.Collinear;
            if (allCollinear)
            {
                double min1X = Math.Min(p1.X, q1.X);
                double max1X = Math.Max(p1.X, q1.X);
                double min2X = Math.Min(p2.X, q2.X);
                double max2X = Math.Max(p2.X, q2.X);

                double min1Y = Math.Min(p1.Y, q1.Y);
                double max1Y = Math.Max(p1.Y, q1.Y);
                double min2Y = Math.Min(p2.Y, q2.Y);
                double max2Y = Math.Max(p2.Y, q2.Y);

                double overlapX = Math.Min(max1X, max2X) - Math.Max(min1X, min2X);
                double overlapY = Math.Min(max1Y, max2Y) - Math.Max(min1Y, min2Y);
                double overlap = Math.Max(overlapX, overlapY);

                return overlap > Geometry2DPredicates.Epsilon;
            }

            return false;
        }

        internal static bool HasSelfIntersection(IReadOnlyList<int> ring, IReadOnlyList<RealPoint2D> points)
        {
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                int iNext = (i + 1) % n;
                var a1 = points[ring[i]];
                var a2 = points[ring[iNext]];

                for (int j = i + 1; j < n; j++)
                {
                    // Skip adjacent edges: (i, i+1) and (0, n-1)
                    if (j == i + 1 || (i == 0 && j == n - 1))
                    {
                        continue;
                    }

                    int jNext = (j + 1) % n;
                    var b1 = points[ring[j]];
                    var b2 = points[ring[jNext]];

                    if (SegmentsIntersectInclusive(in a1, in a2, in b1, in b2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool HasSelfIntersectionProper(IReadOnlyList<int> ring, IReadOnlyList<RealPoint2D> points)
        {
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                int iNext = (i + 1) % n;
                var a1 = points[ring[i]];
                var a2 = points[ring[iNext]];

                for (int j = i + 1; j < n; j++)
                {
                    // Skip adjacent edges: (i, i+1) and (0, n-1)
                    if (j == i + 1 || (i == 0 && j == n - 1))
                    {
                        continue;
                    }

                    int jNext = (j + 1) % n;
                    var b1 = points[ring[j]];
                    var b2 = points[ring[jNext]];

                    if (SegmentsIntersectProper(in a1, in a2, in b1, in b2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool SegmentIntersectsTriangle(
            IReadOnlyList<RealPoint2D> points,
            Edge2D edge,
            Triangle2D triangle)
        {
            var a = points[edge.A];
            var b = points[edge.B];
            var p0 = points[triangle.A];
            var p1 = points[triangle.B];
            var p2 = points[triangle.C];

            if (PointInTriangleInclusive(in a, in p0, in p1, in p2) ||
                PointInTriangleInclusive(in b, in p0, in p1, in p2))
            {
                return true;
            }

            var edges = new (RealPoint2D P, RealPoint2D Q)[] { (p0, p1), (p1, p2), (p2, p0) };
            foreach (var e in edges)
            {
                if (SegmentsIntersectInclusive(in a, in b, in e.P, in e.Q))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool PointInTriangleInclusive(
            in RealPoint2D p,
            in RealPoint2D a,
            in RealPoint2D b,
            in RealPoint2D c)
        {
            var w0 = Geometry2DPredicates.Orientation(in a, in b, in p);
            var w1 = Geometry2DPredicates.Orientation(in b, in c, in p);
            var w2 = Geometry2DPredicates.Orientation(in c, in a, in p);
            bool hasNeg = w0 == OrientationKind.Clockwise || w1 == OrientationKind.Clockwise || w2 == OrientationKind.Clockwise;
            bool hasPos = w0 == OrientationKind.CounterClockwise || w1 == OrientationKind.CounterClockwise || w2 == OrientationKind.CounterClockwise;
            return !(hasNeg && hasPos);
        }

        internal static bool TriangleHasUndirectedEdge(Triangle2D triangle, int a, int b)
        {
            return (triangle.A == a && triangle.B == b) ||
                   (triangle.B == a && triangle.C == b) ||
                   (triangle.C == a && triangle.A == b) ||
                   (triangle.A == b && triangle.B == a) ||
                   (triangle.B == b && triangle.C == a) ||
                   (triangle.C == b && triangle.A == a);
        }

        private static bool OnSegmentInclusive(in RealPoint2D p, in RealPoint2D q, in RealPoint2D r)
        {
            return q.X <= Math.Max(p.X, r.X) + Geometry2DPredicates.Epsilon &&
                   q.X + Geometry2DPredicates.Epsilon >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) + Geometry2DPredicates.Epsilon &&
                   q.Y + Geometry2DPredicates.Epsilon >= Math.Min(p.Y, r.Y);
        }
    }
}
