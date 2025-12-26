namespace Geometry;

/// <summary>
/// Simple line segment represented by two grid points.
/// </summary>
public readonly struct Segment
{
    public Point P0 { get; }
    public Point P1 { get; }

    public Segment(Point p0, Point p1)
    {
        P0 = p0;
        P1 = p1;
    }
}
