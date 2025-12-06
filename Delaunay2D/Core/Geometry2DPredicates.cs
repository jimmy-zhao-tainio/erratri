using Geometry;

namespace Delaunay2D
{
    internal enum OrientationKind
    {
        Collinear = 0,
        CounterClockwise = 1,
        Clockwise = -1
    }

    internal static class Geometry2DPredicates
    {
        internal const double Epsilon = Tolerances.TrianglePredicateEpsilon;

        internal static OrientationKind Orientation(in RealPoint2D p, in RealPoint2D q, in RealPoint2D r)
        {
            double cross = (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
            if (cross > Epsilon) return OrientationKind.CounterClockwise;
            if (cross < -Epsilon) return OrientationKind.Clockwise;
            return OrientationKind.Collinear;
        }

        /// <summary>
        /// Circumcircle test value for point d relative to triangle (a,b,c).
        /// For a CCW (a,b,c), a negative return value means d is strictly inside the circumcircle,
        /// positive means outside, and a magnitude &lt;= epsilon is treated as on the circle.
        /// </summary>
        internal static double CircumcircleValue(
            in RealPoint2D d,
            in RealPoint2D a,
            in RealPoint2D b,
            in RealPoint2D c)
        {
            double ax = a.X - d.X;
            double ay = a.Y - d.Y;
            double bx = b.X - d.X;
            double by = b.Y - d.Y;
            double cx = c.X - d.X;
            double cy = c.Y - d.Y;

            double determinant =
                (ax * ax + ay * ay) * (bx * cy - by * cx) -
                (bx * bx + by * by) * (ax * cy - ay * cx) +
                (cx * cx + cy * cy) * (ax * by - ay * bx);

            if (Orientation(in a, in b, in c) == OrientationKind.Clockwise)
            {
                determinant = -determinant;
            }

            return -determinant;
        }
    }
}
