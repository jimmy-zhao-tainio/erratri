using Geometry;

namespace Pslg;

public readonly struct PslgPoint
{
    public Barycentric Barycentric { get; }

    public PslgPoint(Barycentric barycentric)
    {
        Barycentric = barycentric;
    }
}
