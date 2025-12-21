using System;
using Geometry;

namespace Boolean;

internal static class RayIntersectsTriangleHelper
{
    public static bool RayIntersectsTriangle(
        in RealPoint origin,
        in RealNormal direction,
        in Triangle triangle,
        double maxRayLength)
    {
        var v0 = new RealPoint(triangle.P0);
        var v1 = new RealPoint(triangle.P1);
        var v2 = new RealPoint(triangle.P2);

        var e1 = RealVector.FromPoints(in v0, in v1);
        var e2 = RealVector.FromPoints(in v0, in v2);

        var directionVector = new RealVector(direction.X, direction.Y, direction.Z);
        var pvec = directionVector.Cross(in e2);
        double det = e1.Dot(in pvec);
        double epsilon = Tolerances.TrianglePredicateEpsilon;
        if (Math.Abs(det) < epsilon)
        {
            return false; // Parallel or degenerate.
        }

        double invDet = 1.0 / det;
        var tvec = RealVector.FromPoints(in v0, in origin);

        double u = tvec.Dot(in pvec) * invDet;
        if (u < -epsilon || u > 1.0 + epsilon)
        {
            return false;
        }

        var qvec = tvec.Cross(in e1);
        double v = directionVector.Dot(in qvec) * invDet;
        if (v < -epsilon || u + v > 1.0 + epsilon)
        {
            return false;
        }

        double t = e2.Dot(in qvec) * invDet;
        if (t <= epsilon || t > maxRayLength)
        {
            return false;
        }

        return true;
    }
}
