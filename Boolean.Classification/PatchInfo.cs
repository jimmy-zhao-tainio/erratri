using Geometry;

namespace Boolean;

public readonly struct PatchInfo
{
    public RealTriangle Patch { get; }
    public bool IsInsideOtherMesh { get; }

    public PatchInfo(RealTriangle patch, bool isInsideOtherMesh)
    {
        Patch = patch;
        IsInsideOtherMesh = isInsideOtherMesh;
    }
}
