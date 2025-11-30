using Geometry;

namespace Geometry.Predicates;

public static class RealSegmentPredicates
{
    public static bool TryIntersect(
        RealSegment a,
        RealSegment b,
        out RealPoint intersection)
    {
        double ax = a.Start.X, ay = a.Start.Y;
        double bx = a.End.X,   by = a.End.Y;
        double cx = b.Start.X, cy = b.Start.Y;
        double dx = b.End.X,   dy = b.End.Y;

        double rx = bx - ax;
        double ry = by - ay;
        double sx = dx - cx;
        double sy = dy - cy;

        double rxs = rx * sy - ry * sx;
        double qpx = cx - ax;
        double qpy = cy - ay;

        if (System.Math.Abs(rxs) <= Tolerances.EpsArea)
        {
            intersection = new RealPoint(double.NaN, double.NaN, double.NaN);
            return false;
        }

        double t = (qpx * sy - qpy * sx) / rxs;
        double u = (qpx * ry - qpy * rx) / rxs;

        double epsilon = Tolerances.BarycentricInsideEpsilon;
        if (t < -epsilon || t > 1.0 + epsilon ||
            u < -epsilon || u > 1.0 + epsilon)
        {
            intersection = new RealPoint(double.NaN, double.NaN, double.NaN);
            return false;
        }

        intersection = new RealPoint(ax + t * rx, ay + t * ry, 0.0);
        return true;
    }

    public static bool PointOnSegment(RealPoint p, RealSegment s)
    {
        double cross = (s.End.X - s.Start.X) * (p.Y - s.Start.Y) -
                       (s.End.Y - s.Start.Y) * (p.X - s.Start.X);
        if (System.Math.Abs(cross) > Tolerances.EpsVertex)
        {
            return false;
        }

        double dot = (p.X - s.Start.X) * (s.End.X - s.Start.X) +
                     (p.Y - s.Start.Y) * (s.End.Y - s.Start.Y);
        if (dot < -Tolerances.EpsVertex)
        {
            return false;
        }

        double len2 = (s.End.X - s.Start.X) * (s.End.X - s.Start.X) +
                      (s.End.Y - s.Start.Y) * (s.End.Y - s.Start.Y);
        if (dot - len2 > Tolerances.EpsVertex)
        {
            return false;
        }

        return true;
    }
}
