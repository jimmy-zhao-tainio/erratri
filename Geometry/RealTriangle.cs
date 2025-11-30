namespace Geometry;

public readonly struct RealTriangle
{
    public RealPoint P0 { get; }
    public RealPoint P1 { get; }
    public RealPoint P2 { get; }

    // Centroid of the triangle in 3D.
    public RealPoint Centroid
    {
        get
        {
            double cx = (P0.X + P1.X + P2.X) / 3.0;
            double cy = (P0.Y + P1.Y + P2.Y) / 3.0;
            double cz = (P0.Z + P1.Z + P2.Z) / 3.0;
            return new RealPoint(cx, cy, cz);
        }
    }

    // Signed area in the XY plane.
    public double SignedArea
    {
        get
        {
            return 0.5 * ((P1.X - P0.X) * (P2.Y - P0.Y) - (P1.Y - P0.Y) * (P2.X - P0.X));
        }
    }

    // Magnitude of the true 3D area (0.5 * |AB x AC|).
    public double SignedArea3D
    {
        get
        {
            double abx = P1.X - P0.X;
            double aby = P1.Y - P0.Y;
            double abz = P1.Z - P0.Z;

            double acx = P2.X - P0.X;
            double acy = P2.Y - P0.Y;
            double acz = P2.Z - P0.Z;

            double cxp = aby * acz - abz * acy;
            double cyp = abz * acx - abx * acz;
            double czp = abx * acy - aby * acx;

            double len = Math.Sqrt(cxp * cxp + cyp * cyp + czp * czp);
            return 0.5 * len;
        }
    }

    public RealTriangle(RealPoint p0, RealPoint p1, RealPoint p2)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;
    }

    public RealTriangle(Triangle triangle)
    {
        P0 = new RealPoint(triangle.P0.X, triangle.P0.Y, triangle.P0.Z);
        P1 = new RealPoint(triangle.P1.X, triangle.P1.Y, triangle.P1.Z);
        P2 = new RealPoint(triangle.P2.X, triangle.P2.Y, triangle.P2.Z);
    }

    // Compute barycentric coordinates (U, V, W) for a real-valued point in
    // the plane of this triangle, together with the raw denominator used by
    // the dot-product system.
    internal Barycentric ComputeBarycentric(in RealPoint point, out double denom)
    {
        var p0 = new RealVector(P0.X, P0.Y, P0.Z);
        var p1 = new RealVector(P1.X, P1.Y, P1.Z);
        var p2 = new RealVector(P2.X, P2.Y, P2.Z);

        var v0 = p1 - p0;
        var v1 = p2 - p0;
        var pointVector = new RealVector(point.X, point.Y, point.Z);
        var v2 = pointVector - p0;

        double d00 = v0.Dot(v0);
        double d01 = v0.Dot(v1);
        double d11 = v1.Dot(v1);
        double d20 = v2.Dot(v0);
        double d21 = v2.Dot(v1);

        denom = d00 * d11 - d01 * d01;
        if (denom == 0.0)
            return new Barycentric(0.0, 0.0, 0.0);

        double invDenom = 1.0 / denom;
        double v = (d11 * d20 - d01 * d21) * invDenom;
        double w = (d00 * d21 - d01 * d20) * invDenom;
        double u = 1.0 - v - w;
        return new Barycentric(u, v, w);
    }
}
