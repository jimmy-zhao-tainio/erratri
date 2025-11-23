namespace Geometry;

public readonly struct RealSegment
{
    public RealPoint Start { get; }
    public RealPoint End   { get; }

    public RealSegment(RealPoint start, RealPoint end)
    {
        Start = start;
        End = end;
    }
}
