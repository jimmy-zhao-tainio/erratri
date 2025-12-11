using System;
using System.Collections.Generic;
using Geometry;

namespace ConstrainedTriangulator
{
    internal static class Enforce
    {
        internal static bool IsLegalTriangle(
            int a, int b, int c,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            if (!IsEdgeAllowed(a, b, points, segments)) return false;
            if (!IsEdgeAllowed(b, c, points, segments)) return false;
            if (!IsEdgeAllowed(c, a, points, segments)) return false;

            if (ContainsInteriorPoint(a, b, c, points)) return false;

            return true;
        }

        private static bool IsEdgeAllowed(
            int a, int b,
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            if (a == b) return false;

            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                if ((s.A == a && s.B == b) || (s.A == b && s.B == a))
                    return true;
            }

            if (Geometry.EdgeCrossesExisting(a, b, points, segments))
                return false;

            return true;
        }

        internal static bool ContainsInteriorPoint(
            int a, int b, int c,
            IReadOnlyList<RealPoint2D> points)
        {
            var pa = points[a];
            var pb = points[b];
            var pc = points[c];

            for (int i = 0; i < points.Count; i++)
            {
                if (i == a || i == b || i == c)
                {
                    continue;
                }

                var p = points[i];
                if (IsStrictlyInsideTriangle(in p, in pa, in pb, in pc))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStrictlyInsideTriangle(
            in RealPoint2D p,
            in RealPoint2D a,
            in RealPoint2D b,
            in RealPoint2D c)
        {
            double w0 = Orientation(in a, in b, in p);
            double w1 = Orientation(in b, in c, in p);
            double w2 = Orientation(in c, in a, in p);

            bool hasNeg = w0 < -Tolerances.EpsArea || w1 < -Tolerances.EpsArea || w2 < -Tolerances.EpsArea;
            bool hasPos = w0 > Tolerances.EpsArea || w1 > Tolerances.EpsArea || w2 > Tolerances.EpsArea;

            bool onEdge = Math.Abs(w0) <= Tolerances.EpsArea || Math.Abs(w1) <= Tolerances.EpsArea || Math.Abs(w2) <= Tolerances.EpsArea;
            return !(hasNeg && hasPos) && !onEdge;
        }

        private static double Orientation(in RealPoint2D p, in RealPoint2D q, in RealPoint2D r)
        {
            return (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
        }
    }
}
