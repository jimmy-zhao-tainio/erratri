namespace Pslg;

public readonly struct PslgSegment
{
    public int StartIndex { get; }
    public int EndIndex { get; }

    public PslgSegment(int startIndex, int endIndex)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
    }
}
