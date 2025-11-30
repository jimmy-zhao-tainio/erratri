namespace Geometry;

// 2D point in real (double) coordinates, used for projected
// geometry in the XY-like planes. Mirrors the simple X/Y layout
// of the former PairIntersectionMath.Point2D type.
public readonly struct RealPoint2D
{
    public readonly double X;
    public readonly double Y;

    public RealPoint2D(double x, double y)
    {
        X = x;
        Y = y;
    }
}

