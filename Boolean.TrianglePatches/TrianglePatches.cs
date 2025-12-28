using System.Collections.Generic;

namespace Boolean;

public sealed class TrianglePatches
{
    // Patches for each triangle in IntersectionSet.TrianglesA.
    // Index i corresponds to triangle i in mesh A.
    public IReadOnlyList<IReadOnlyList<TrianglePatch>> TrianglesA { get; }

    // Patches for each triangle in IntersectionSet.TrianglesB.
    // Index j corresponds to triangle j in mesh B.
    public IReadOnlyList<IReadOnlyList<TrianglePatch>> TrianglesB { get; }

    public TrianglePatches(
        IReadOnlyList<IReadOnlyList<TrianglePatch>> trianglesA,
        IReadOnlyList<IReadOnlyList<TrianglePatch>> trianglesB)
    {
        TrianglesA = trianglesA ?? throw new System.ArgumentNullException(nameof(trianglesA));
        TrianglesB = trianglesB ?? throw new System.ArgumentNullException(nameof(trianglesB));
    }
}
