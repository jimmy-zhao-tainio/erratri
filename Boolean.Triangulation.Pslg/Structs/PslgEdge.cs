namespace Pslg;

public readonly struct PslgEdge
{
    public int Start { get; }
    public int End { get; }

    // True if this edge lies on the triangle boundary.
    public bool IsBoundary { get; }

    public PslgEdge(int start, int end, bool isBoundary)
    {
        Start = start;
        End = end;
        IsBoundary = isBoundary;
    }
}
