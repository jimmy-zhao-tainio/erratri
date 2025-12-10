using System;
using System.Collections.Generic;
using Geometry;

namespace TriangleGarden
{
    internal static class TriangleGardenGeometry
    {
        internal static bool EdgeCrossesExisting(
            int a,
            int b,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            var p1 = points[a];
            var p2 = points[b];

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                if (SharesEndpoint(seg, a, b))
                {
                    continue;
                }

                var q1 = points[seg.A];
                var q2 = points[seg.B];
                if (SegmentsIntersectProper(in p1, in p2, in q1, in q2))
                {
                    return true;
                }
            }

            return false;
        }

        internal static double TriangleArea(RealPoint2D p1, RealPoint2D p2, RealPoint2D p3)
        {
            return 0.5 * ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X));
        }

        internal static OrientationKind Orientation(in RealPoint2D p, in RealPoint2D q, in RealPoint2D r)
        {
            double cross = (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
            if (cross > Tolerances.TrianglePredicateEpsilon) return OrientationKind.CounterClockwise;
            if (cross < -Tolerances.TrianglePredicateEpsilon) return OrientationKind.Clockwise;
            return OrientationKind.Collinear;
        }

        private static bool SharesEndpoint((int A, int B) seg, int a, int b)
        {
            return seg.A == a || seg.B == a || seg.A == b || seg.B == b;
        }

        private static bool SegmentsIntersectProper(
            in RealPoint2D p1,
            in RealPoint2D q1,
            in RealPoint2D p2,
            in RealPoint2D q2)
        {
            var o1 = Orientation(in p1, in q1, in p2);
            var o2 = Orientation(in p1, in q1, in q2);
            var o3 = Orientation(in p2, in q2, in p1);
            var o4 = Orientation(in p2, in q2, in q1);

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

                return overlap > Tolerances.EpsArea;
            }

            return false;
        }
    }

    internal enum OrientationKind
    {
        Collinear = 0,
        CounterClockwise = 1,
        Clockwise = -1
    }
}
