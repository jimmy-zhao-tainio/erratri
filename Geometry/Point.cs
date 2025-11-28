namespace Geometry;

public readonly struct Point
{
    public readonly long X;
    public readonly long Y;
    public readonly long Z;

    public Point(long x, long y, long z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Point operator +(Point p, (long dx, long dy, long dz) d)
        => new Point(p.X + d.dx, p.Y + d.dy, p.Z + d.dz);
}
