using Contracts.Core;

namespace Contracts.Stage0to1;

public sealed record Stage0To1Context
{
    public required MeshRef LeftMesh { get; init; }
    public required MeshRef RightMesh { get; init; }
    public required BooleanOperation Op { get; init; }
    public required ToleranceBundle Tolerances { get; init; }
    public required CoordinateSpace CoordinateSpace { get; init; }
    public required DeterminismPolicy DeterminismPolicy { get; init; }
}

