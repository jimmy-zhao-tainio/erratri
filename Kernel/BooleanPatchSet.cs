using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Output of the boolean patch selection step: which patches from each mesh
// should participate in the chosen boolean operation.
public sealed class BooleanPatchSet
{
    public IReadOnlyList<RealTriangle> FromMeshA { get; }
    public IReadOnlyList<RealTriangle> FromMeshB { get; }

    public BooleanPatchSet(
        IReadOnlyList<RealTriangle> fromMeshA,
        IReadOnlyList<RealTriangle> fromMeshB)
    {
        FromMeshA = fromMeshA ?? throw new ArgumentNullException(nameof(fromMeshA));
        FromMeshB = fromMeshB ?? throw new ArgumentNullException(nameof(fromMeshB));
    }
}
