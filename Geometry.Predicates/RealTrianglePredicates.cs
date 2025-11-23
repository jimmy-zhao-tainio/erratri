using Geometry;

namespace Geometry.Predicates;

public static class RealTrianglePredicates
{
    public static bool IsInsideStrict(RealTriangle tri, RealPoint p)
    {
        double t1 = (tri.P1.X - tri.P0.X) * (p.Y - tri.P0.Y) -
                    (tri.P1.Y - tri.P0.Y) * (p.X - tri.P0.X);
        double t2 = (tri.P2.X - tri.P1.X) * (p.Y - tri.P1.Y) -
                    (tri.P2.Y - tri.P1.Y) * (p.X - tri.P1.X);
        double t3 = (tri.P0.X - tri.P2.X) * (p.Y - tri.P2.Y) -
                    (tri.P0.Y - tri.P2.Y) * (p.X - tri.P2.X);

        return t1 > Tolerances.EpsArea && t2 > Tolerances.EpsArea && t3 > Tolerances.EpsArea;
    }
}
