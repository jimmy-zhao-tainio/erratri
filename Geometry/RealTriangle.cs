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
}
