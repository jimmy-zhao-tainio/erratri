namespace Boolean;

public readonly struct IntersectionSegment
{
    public int StartIndex { get; }
    public int EndIndex { get; }

    public IntersectionSegment(int startIndex, int endIndex)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
    }
}
