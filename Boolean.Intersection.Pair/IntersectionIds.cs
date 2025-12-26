namespace Boolean;

public readonly struct IntersectionVertexId
{
    public int Value { get; }

    public IntersectionVertexId(int value)
    {
        Value = value;
    }

    public override string ToString() => $"v{Value}";
}

public readonly struct IntersectionEdgeId
{
    public int Value { get; }

    public IntersectionEdgeId(int value)
    {
        Value = value;
    }
}
