namespace Geometry;

// Barycentric coordinates (U, V, W) for points on a triangle.
// Used only for parametrization; robust predicates and intersection
// classification remain in the integer grid layer.
public readonly struct Barycentric
{
    public readonly double U;
    public readonly double V;
    public readonly double W;

    public Barycentric(double u, double v, double w)
    {
        U = u;
        V = v;
        W = w;
    }

    // Compute barycentric coordinates for an integer-grid point on a triangle.
    // This uses the same dot-product scheme and degeneracy handling as the
    // former Triangle.ToBarycentric(Point) helper.
    public static Barycentric FromPointOnTriangle(in Triangle triangle, in Point point)
    {
        var realTriangle = new RealTriangle(
            new RealPoint(triangle.P0.X, triangle.P0.Y, triangle.P0.Z),
            new RealPoint(triangle.P1.X, triangle.P1.Y, triangle.P1.Z),
            new RealPoint(triangle.P2.X, triangle.P2.Y, triangle.P2.Z));

        var asPoint = new RealPoint(point.X, point.Y, point.Z);
        var barycentric = realTriangle.ComputeBarycentric(in asPoint, out double denom);
        if (denom == 0.0)
        {
            // Degenerate triangle metric; should not happen for valid input.
            // Callers should treat this as an error path and expect the
            // resulting barycentric to be aggressively merged.
            System.Diagnostics.Debug.Assert(false, "Degenerate triangle in ToBarycentric.");
            return new Barycentric(0.0, 0.0, 0.0);
        }

        return barycentric;
    }

    // Reconstruct a real-valued point from barycentric coordinates with
    // respect to a triangle in integer grid space.
    public static RealPoint ToRealPointOnTriangle(in Triangle triangle, in Barycentric barycentric)
    {
        var u = barycentric.U;
        var v = barycentric.V;
        var w = barycentric.W;

        var x = u * triangle.P0.X + v * triangle.P1.X + w * triangle.P2.X;
        var y = u * triangle.P0.Y + v * triangle.P1.Y + w * triangle.P2.Y;
        var z = u * triangle.P0.Z + v * triangle.P1.Z + w * triangle.P2.Z;

        return new RealPoint(x, y, z);
    }

    // Inclusive test for being inside or on the boundary of the
    // reference triangle, with a small tolerance on the barycentric
    // constraints U, V, W >= 0 and U + V + W == 1.
    public bool IsInsideInclusive()
    {
        const double epsilon = Tolerances.BarycentricInsideEpsilon;

        if (U < -epsilon || V < -epsilon || W < -epsilon)
            return false;

        var sum = U + V + W;
        return System.Math.Abs(sum - 1.0) <= epsilon;
    }

    // Component-wise closeness between two barycentric coordinates using
    // the shared feature-layer tolerance from Geometry.Tolerances.
    public bool IsCloseTo(in Barycentric other)
    {
        double epsilon = Tolerances.FeatureBarycentricEpsilon;
        return System.Math.Abs(U - other.U) <= epsilon &&
               System.Math.Abs(V - other.V) <= epsilon &&
               System.Math.Abs(W - other.W) <= epsilon;
    }
}
