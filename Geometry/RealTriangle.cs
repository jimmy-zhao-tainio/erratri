namespace Geometry;

public readonly struct RealTriangle
{
    public RealPoint P0 { get; }
    public RealPoint P1 { get; }
    public RealPoint P2 { get; }

    // Signed area in the XY plane.
    public double SignedArea
    {
        get
        {
            return 0.5 * ((P1.X - P0.X) * (P2.Y - P0.Y) - (P1.Y - P0.Y) * (P2.X - P0.X));
        }
    }

    public RealTriangle(RealPoint p0, RealPoint p1, RealPoint p2)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;
    }
}
