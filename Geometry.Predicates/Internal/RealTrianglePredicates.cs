using Geometry;

namespace Geometry.Predicates.Internal;

internal static class RealTrianglePredicates
{
    internal static bool IsPointInsidePredicate(in RealTriangle tri, in RealPoint point)
    {
        var bary = tri.ComputeBarycentric(in point, out double denom);
        double eps = Tolerances.TrianglePredicateEpsilon;
        if (System.Math.Abs(denom) < eps) return false;
        if (bary.U < -eps || bary.V < -eps || bary.W < -eps) return false;
        return true;
    }
}

