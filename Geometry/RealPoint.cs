namespace Geometry;

public readonly struct RealPoint
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public RealPoint(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public RealPoint(Point p)
    {
        X = p.X;
        Y = p.Y;
        Z = p.Z;
    }

    public double DistanceSquared(in RealPoint other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    public double Distance(in RealPoint other)
        => Math.Sqrt(DistanceSquared(in other));
}
