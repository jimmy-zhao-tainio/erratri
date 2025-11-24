using System;
using Geometry;

namespace Kernel;

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

        var dirVec = new RealVector(direction.X, direction.Y, direction.Z);
        var pvec = dirVec.Cross(in e2);
        double det = e1.Dot(in pvec);
        double eps = Tolerances.TrianglePredicateEpsilon;
        if (Math.Abs(det) < eps)
        {
            return false; // Parallel or degenerate.
        }

        double invDet = 1.0 / det;
        var tvec = RealVector.FromPoints(in v0, in origin);

        double u = tvec.Dot(in pvec) * invDet;
        if (u < -eps || u > 1.0 + eps)
        {
            return false;
        }

        var qvec = tvec.Cross(in e1);
        double v = dirVec.Dot(in qvec) * invDet;
        if (v < -eps || u + v > 1.0 + eps)
        {
            return false;
        }

        double t = e2.Dot(in qvec) * invDet;
        if (t <= eps || t > maxRayLength)
        {
            return false;
        }

        return true;
    }
}
