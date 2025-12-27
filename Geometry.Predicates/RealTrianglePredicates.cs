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

    public static bool IsOnTriangle(
        RealTriangle triangle,
        RealPoint point,
        double epsDist,
        double epsBary)
    {
        var p0 = triangle.P0;
        var p1 = triangle.P1;
        var p2 = triangle.P2;
        var e1 = RealVector.FromPoints(in p0, in p1);
        var e2 = RealVector.FromPoints(in p0, in p2);
        var normal = e1.Cross(in e2);
        double len = normal.Length();
        if (len <= epsDist)
        {
            return false;
        }

        var v = RealVector.FromPoints(in p0, in point);
        double distance = System.Math.Abs(normal.Dot(in v)) / len;
        if (distance > epsDist)
        {
            return false;
        }

        var barycentric = triangle.ComputeBarycentric(in point, out double denominator);
        if (System.Math.Abs(denominator) < Tolerances.TrianglePredicateEpsilon)
        {
            return false;
        }

        if (barycentric.U < -epsBary || barycentric.V < -epsBary || barycentric.W < -epsBary)
        {
            return false;
        }

        if (barycentric.U > 1.0 + epsBary || barycentric.V > 1.0 + epsBary || barycentric.W > 1.0 + epsBary)
        {
            return false;
        }

        return true;
    }
}
