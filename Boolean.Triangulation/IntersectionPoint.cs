using Geometry;

namespace Boolean;

public readonly struct IntersectionPoint
{
    public Barycentric Barycentric { get; }
    public RealPoint Position { get; }

    public IntersectionPoint(Barycentric barycentric, RealPoint position)
    {
        Barycentric = barycentric;
        Position = position;
    }
}
