using Contracts.Core;

namespace Contracts.Stage0to1;

public static class Stage0to1Validators
{
    public static void ValidateBooleanOpRequestStrict(BooleanOpRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var ctx = new ContractValidationContext();

        ValidateMeshRefInput(ctx, request.LeftMesh, "leftMesh", "BP01.REQUEST.LEFT_MESH_ID_INVALID", "1.1");
        ValidateMeshRefInput(ctx, request.RightMesh, "rightMesh", "BP01.REQUEST.RIGHT_MESH_ID_INVALID", "1.2");

        if (!Enum.IsDefined(typeof(BooleanOperation), request.Op))
            ctx.Add(ContractErrorCode.From("BP01.REQUEST.OP_INVALID"), "Invariant 1.3: Op must be a supported boolean operation.", "op");

        if (!Enum.IsDefined(typeof(DeterminismPolicy), request.DeterminismPolicy))
            ctx.Add(ContractErrorCode.From("BP01.REQUEST.DETERMINISM_POLICY_INVALID"), "Invariant 1.4: DeterminismPolicy is invalid.", "determinismPolicy");

        if (!Enum.IsDefined(typeof(CoordinateSpace), request.CoordinateSpace))
            ctx.Add(ContractErrorCode.From("BP01.REQUEST.COORDINATE_SPACE_INVALID"), "Invariant 1.5: CoordinateSpace is invalid.", "coordinateSpace");

        if (request.Tolerances is null)
        {
            ctx.Add(ContractErrorCode.From("BP01.REQUEST.TOLERANCES_MISSING"), "Invariant 1.6: Tolerances are required.", "tolerances");
        }
        else
        {
            request.Tolerances.ValidateStrict(ctx, "tolerances");
        }

        ctx.ThrowIfAny();
    }

    public static void ValidateStage0To1ContextStrict(Stage0To1Context context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        var ctx = new ContractValidationContext();

        ValidateMeshRefOutput(ctx, context.LeftMesh, "leftMesh", "BP01.CONTEXT.LEFT_MESH_ID_NOT_CANONICAL", "2.1");
        ValidateMeshRefOutput(ctx, context.RightMesh, "rightMesh", "BP01.CONTEXT.RIGHT_MESH_ID_NOT_CANONICAL", "2.2");

        if (!Enum.IsDefined(typeof(BooleanOperation), context.Op))
            ctx.Add(ContractErrorCode.From("BP01.CONTEXT.OP_INVALID"), "Invariant 2.3: Op must be a supported boolean operation.", "op");

        if (!Enum.IsDefined(typeof(DeterminismPolicy), context.DeterminismPolicy))
            ctx.Add(ContractErrorCode.From("BP01.CONTEXT.DETERMINISM_POLICY_INVALID"), "Invariant 2.4: DeterminismPolicy is invalid.", "determinismPolicy");

        if (!Enum.IsDefined(typeof(CoordinateSpace), context.CoordinateSpace))
            ctx.Add(ContractErrorCode.From("BP01.CONTEXT.COORDINATE_SPACE_INVALID"), "Invariant 2.5: CoordinateSpace is invalid.", "coordinateSpace");

        if (context.Tolerances is null)
        {
            ctx.Add(ContractErrorCode.From("BP01.CONTEXT.TOLERANCES_MISSING"), "Invariant 2.6: Tolerances are required.", "tolerances");
        }
        else
        {
            context.Tolerances.ValidateStrict(ctx, "tolerances");
        }

        ctx.ThrowIfAny();
    }

    private static void ValidateMeshRefInput(
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
        }
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
