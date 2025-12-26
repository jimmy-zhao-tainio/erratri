using System.Collections.Generic;
using Geometry;

namespace Boolean;

public sealed class TrianglePatches
{
    // Patches for each triangle in IntersectionSet.TrianglesA.
    // Index i corresponds to triangle i in mesh A.
    public IReadOnlyList<IReadOnlyList<RealTriangle>> TrianglesA { get; }

    // Patches for each triangle in IntersectionSet.TrianglesB.
    // Index j corresponds to triangle j in mesh B.
    public IReadOnlyList<IReadOnlyList<RealTriangle>> TrianglesB { get; }

    public TrianglePatches(
        IReadOnlyList<IReadOnlyList<RealTriangle>> trianglesA,
        IReadOnlyList<IReadOnlyList<RealTriangle>> trianglesB)
    {
        TrianglesA = trianglesA ?? throw new System.ArgumentNullException(nameof(trianglesA));
        TrianglesB = trianglesB ?? throw new System.ArgumentNullException(nameof(trianglesB));
    }
}
