using Contracts.Core;
using Contracts.Stage0to1;

namespace Contracts.Stage1to2;

public static class Stage1to2Validators
{
    public static void ValidateStage1To2ContextStrict(Stage1To2Context context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        var ctx = new ContractValidationContext();

        ValidateMeshRefOutput(ctx, context.LeftMesh, "leftMesh", "BP02.CONTEXT.LEFT_MESH_ID_NOT_CANONICAL", "1.1");
        ValidateMeshRefOutput(ctx, context.RightMesh, "rightMesh", "BP02.CONTEXT.RIGHT_MESH_ID_NOT_CANONICAL", "1.2");

        if (!Enum.IsDefined(typeof(BooleanOperation), context.Op))
            ctx.Add(ContractErrorCode.From("BP02.CONTEXT.OP_INVALID"), "Invariant 1.3: Op must be a defined enum constant.", "op");

        if (!Enum.IsDefined(typeof(DeterminismPolicy), context.DeterminismPolicy))
            ctx.Add(ContractErrorCode.From("BP02.CONTEXT.DETERMINISM_POLICY_INVALID"), "Invariant 1.4: DeterminismPolicy is invalid.", "determinismPolicy");

        if (!Enum.IsDefined(typeof(CoordinateSpace), context.CoordinateSpace))
            ctx.Add(ContractErrorCode.From("BP02.CONTEXT.COORDINATE_SPACE_INVALID"), "Invariant 1.5: CoordinateSpace is invalid.", "coordinateSpace");

        if (context.Tolerances is null)
        {
            ctx.Add(ContractErrorCode.From("BP02.CONTEXT.TOLERANCES_MISSING"), "Invariant 1.6: Tolerances are required.", "tolerances");
        }
        else
        {
            context.Tolerances.ValidateStrict(ctx, "tolerances");
        }

        ctx.ThrowIfAny();
    }

    private static void ValidateMeshRefOutput(
        ContractValidationContext ctx,
        MeshRef? mesh,
        string path,
        string errorCode,
        string invariantId)
    {
        if (mesh is null)
        {
            ctx.Add(ContractErrorCode.From(errorCode), $"Invariant {invariantId}: Mesh reference is required.", path);
            return;
        }

        if (string.IsNullOrWhiteSpace(mesh.Id) || string.IsNullOrWhiteSpace(mesh.Id.Trim()))
        {
            ctx.Add(ContractErrorCode.From(errorCode), $"Invariant {invariantId}: Mesh Id must be non-empty after trimming.", $"{path}.id");
            return;
        }

        if (!string.Equals(mesh.Id, mesh.Id.Trim(), StringComparison.Ordinal))
        {
            ctx.Add(ContractErrorCode.From(errorCode), $"Invariant {invariantId}: Mesh Id must be canonical (no leading/trailing whitespace).", $"{path}.id");
        }
    }
}

