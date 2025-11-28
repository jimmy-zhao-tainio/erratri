namespace Geometry;

// Lightweight plane representation built from a triangle.
// Uses the triangle's outward unit normal and a reference point on the plane.
public readonly struct Plane
{
    public readonly RealNormal Normal; // unit length
    public readonly Point Point;   // reference point (on plane)

    public Plane(RealNormal normal, Point point)
    {
        Normal = normal;
        Point = point;
    }

    public static Plane FromTriangle(in Triangle tri)
        => new Plane(tri.Normal, tri.P0);

    // Signed evaluation of point relative to plane.
    // >0: positive side (along normal), <0: negative side (inside for outward-oriented faces)
    public double Evaluate(in Point p)
    {
        var dx = (double)p.X - Point.X;
        var dy = (double)p.Y - Point.Y;
        var dz = (double)p.Z - Point.Z;
        return Normal.X * dx + Normal.Y * dy + Normal.Z * dz;
    }

    // Classify point side using Geometry.Tolerances.PlaneSideEpsilon.
    // Returns 1 (positive), -1 (negative), or 0 (on plane)
    public int Side(in Point p, double eps = Tolerances.PlaneSideEpsilon)
    {
        var s = Evaluate(p);
        if (s > eps) return 1;
        if (s < -eps) return -1;
        return 0;
    }
}
