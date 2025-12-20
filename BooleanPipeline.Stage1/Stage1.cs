using Contracts.Stage0to1;
using Contracts.Stage1to2;

namespace BooleanPipeline.Stage1;

public static class Stage1
{
    public static Stage1To2Context Run(Stage0To1Context context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return new Stage1To2Context
        {
            LeftMesh = new MeshRef { Id = context.LeftMesh.Id.Trim() },
            RightMesh = new MeshRef { Id = context.RightMesh.Id.Trim() },
            Op = context.Op,
            Tolerances = context.Tolerances,
            CoordinateSpace = context.CoordinateSpace,
            DeterminismPolicy = context.DeterminismPolicy,
        };
    }
}

