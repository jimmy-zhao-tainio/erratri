using Contracts.Core;
using Contracts.Stage0to1;

namespace Contracts.Stage1to2;

public sealed record Stage1To2Context
{
    public required MeshRef LeftMesh { get; init; }
    public required MeshRef RightMesh { get; init; }
    public required BooleanOperation Op { get; init; }
    public required ToleranceBundle Tolerances { get; init; }
    public required CoordinateSpace CoordinateSpace { get; init; }
    public required DeterminismPolicy DeterminismPolicy { get; init; }
}
