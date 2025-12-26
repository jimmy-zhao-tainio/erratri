namespace Geometry;

// Utility for snapping real-valued coordinates back to the integer
// grid used by Point. All conversions between real space (double)
// and grid space (long) should go through this type.
public static class GridRounding
{
    public static long Snap(double value)
        => (long)Math.Round(value, MidpointRounding.AwayFromZero);

    public static Point Snap(RealPoint point)
        => new Point(
            Snap(point.X),
            Snap(point.Y),
            Snap(point.Z));
}
