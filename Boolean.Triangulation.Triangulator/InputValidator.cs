using System;
using Geometry;

namespace ConstrainedTriangulator
{
    internal static class InputValidator
    {
        internal static void Validate(in Input input)
        {
            if (input.Points is null)
                throw new ArgumentNullException(nameof(input.Points));

            if (input.Segments is null)
                throw new ArgumentNullException(nameof(input.Segments));

            int pointCount = input.Points.Count;
            if (pointCount < 3)
                throw new ArgumentException("Need at least 3 points for triangulation.", nameof(input));

            // Duplicate/near-duplicate point check
            for (int i = 0; i < pointCount; i++)
            {
                var pi = input.Points[i];
                for (int j = i + 1; j < pointCount; j++)
                {
                    var pj = input.Points[j];
                    double dx = pi.X - pj.X;
                    double dy = pi.Y - pj.Y;
                    double dist2 = dx * dx + dy * dy;
                    if (dist2 <= Tolerances.EpsVertexSquared)
                    {
                        throw new ArgumentException("ConstrainedTriangulatorInput contains duplicate or near-duplicate points.", nameof(input));
                    }
                }
            }

            var first = input.Points[0];
            var second = input.Points[1];
            bool allCollinear = true;
            for (int i = 2; i < pointCount; i++)
            {
                var current = input.Points[i];
                double orient = (second.X - first.X) * (current.Y - first.Y) - (second.Y - first.Y) * (current.X - first.X);
                if (Math.Abs(orient) > Tolerances.EpsArea)
                {
                    allCollinear = false;
                    break;
                }
            }

            if (allCollinear)
            {
                throw new ArgumentException("Cannot triangulate: all points are collinear within tolerance.", nameof(input));
            }

            var segments = input.Segments;

            // Indices in range and non-degenerate segments
            for (int i = 0; i < segments.Count; i++)
            {
                var (a, b) = segments[i];

                if ((uint)a >= (uint)pointCount || (uint)b >= (uint)pointCount)
                    throw new ArgumentException("Segment index out of range.", nameof(input));

                if (a == b)
                    throw new ArgumentException("Segment has identical endpoints.", nameof(input));
            }

            // PSLG check: no segmentâ€“segment intersections except shared endpoints
            for (int i = 0; i < segments.Count; i++)
            {
                var (a1, b1) = segments[i];
                var p1 = input.Points[a1];
                var p2 = input.Points[b1];

                for (int j = i + 1; j < segments.Count; j++)
                {
                    var (a2, b2) = segments[j];

                    // shared endpoint is allowed
                    if (a1 == a2 || a1 == b2 || b1 == a2 || b1 == b2)
                        continue;

                    var q1 = input.Points[a2];
                    var q2 = input.Points[b2];

                    if (ProperSegmentsIntersect(p1, p2, q1, q2))
                    {
                        throw new ArgumentException("Input segments self-intersect (not a valid PSLG).", nameof(input));
                    }
                }
            }
        }

        private static bool ProperSegmentsIntersect(RealPoint2D p1, RealPoint2D p2, RealPoint2D q1, RealPoint2D q2)
        {
            double o1 = Orientation(p1, p2, q1);
            double o2 = Orientation(p1, p2, q2);
            double o3 = Orientation(q1, q2, p1);
            double o4 = Orientation(q1, q2, p2);

            // strict intersection (no endpoints); if you want eps, plug Tolerances.EpsArea in here
            return o1 * o2 < 0.0 && o3 * o4 < 0.0;
        }

        private static double Orientation(RealPoint2D a, RealPoint2D b, RealPoint2D c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }
    }
}
