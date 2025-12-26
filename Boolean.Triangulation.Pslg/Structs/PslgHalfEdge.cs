namespace Pslg;

public struct PslgHalfEdge
{
    public int From { get; set; }
    public int To { get; set; }
    public int Twin { get; set; }
    public int Next { get; set; }
    public bool IsBoundary { get; set; }
}
