using System.Collections.Generic;

namespace Boolean;

public sealed class PatchClassification
{
    public IReadOnlyList<IReadOnlyList<PatchInfo>> MeshA { get; }
    public IReadOnlyList<IReadOnlyList<PatchInfo>> MeshB { get; }

    public PatchClassification(
        IReadOnlyList<IReadOnlyList<PatchInfo>> meshA,
        IReadOnlyList<IReadOnlyList<PatchInfo>> meshB)
    {
        MeshA = meshA ?? throw new System.ArgumentNullException(nameof(meshA));
        MeshB = meshB ?? throw new System.ArgumentNullException(nameof(meshB));
    }
}
