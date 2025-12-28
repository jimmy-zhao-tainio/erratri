namespace Boolean;

public readonly struct TriangleVertexIds
{
    public int V0 { get; }
    public int V1 { get; }
    public int V2 { get; }

    public TriangleVertexIds(int v0, int v1, int v2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
    }
}
