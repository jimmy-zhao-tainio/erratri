using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

// Output of the boolean patch selection step: which patches from each mesh
// should participate in the chosen boolean operation.
public sealed class BooleanPatchSet
{
    public IReadOnlyList<RealTriangle> FromMeshA { get; }
    public IReadOnlyList<RealTriangle> FromMeshB { get; }

    // Optional debug-only provenance (parallel to FromMeshA / FromMeshB).
    public IReadOnlyList<string>? ProvenanceFromMeshA { get; }
    public IReadOnlyList<string>? ProvenanceFromMeshB { get; }

    public BooleanPatchSet(
        IReadOnlyList<RealTriangle> fromMeshA,
        IReadOnlyList<RealTriangle> fromMeshB)
    {
        FromMeshA = fromMeshA ?? throw new ArgumentNullException(nameof(fromMeshA));
        FromMeshB = fromMeshB ?? throw new ArgumentNullException(nameof(fromMeshB));
    }

    public BooleanPatchSet(
        IReadOnlyList<RealTriangle> fromMeshA,
        IReadOnlyList<RealTriangle> fromMeshB,
        IReadOnlyList<string> provenanceFromMeshA,
        IReadOnlyList<string> provenanceFromMeshB)
        : this(fromMeshA, fromMeshB)
    {
        ProvenanceFromMeshA = provenanceFromMeshA ?? throw new ArgumentNullException(nameof(provenanceFromMeshA));
        ProvenanceFromMeshB = provenanceFromMeshB ?? throw new ArgumentNullException(nameof(provenanceFromMeshB));
    }
}
