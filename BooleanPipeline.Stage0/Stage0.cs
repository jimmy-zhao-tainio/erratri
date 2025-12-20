using Contracts.Stage0to1;

namespace BooleanPipeline.Stage0;

public static class Stage0
{
    public static Stage0To1Context Run(BooleanOpRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        return new Stage0To1Context
        {
            LeftMesh = new MeshRef { Id = request.LeftMesh.Id.Trim() },
            RightMesh = new MeshRef { Id = request.RightMesh.Id.Trim() },
            Op = request.Op,
            Tolerances = request.Tolerances,
            CoordinateSpace = request.CoordinateSpace,
            DeterminismPolicy = request.DeterminismPolicy,
        };
    }
}

