using Geometry;

namespace Geometry.Predicates;

public static class RealTrianglePredicates
{
    // 3D barycentric point-in-triangle predicate.
    // Uses the same epsilon semantics as the legacy implementations:
    //   - degenerate if |denominator| < TrianglePredicateEpsilon,
    //   - inside if U, V, W are all >= -TrianglePredicateEpsilon.
    public static bool IsInsideStrict(RealTriangle triangle, RealPoint point)
    {
        var barycentric = triangle.ComputeBarycentric(in point, out double denominator);
        double epsilon = Tolerances.TrianglePredicateEpsilon;
        if (System.Math.Abs(denominator) < epsilon) return false;
        if (barycentric.U < -epsilon || barycentric.V < -epsilon || barycentric.W < -epsilon) return false;
        return true;
    }
}
