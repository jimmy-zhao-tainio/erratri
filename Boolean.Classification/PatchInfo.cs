using Geometry;

namespace Boolean;

public readonly struct PatchInfo
{
    public RealTriangle Patch { get; }
    public Containment Containment { get; }

    public PatchInfo(RealTriangle patch, Containment containment)
    {
        Patch = patch;
        Containment = containment;
    }
}
