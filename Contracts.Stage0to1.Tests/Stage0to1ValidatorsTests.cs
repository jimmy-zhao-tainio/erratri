using Contracts.Core;
using Contracts.Stage0to1;
using Contracts.Testing;

namespace Contracts.Stage0to1.Tests;

public sealed class Stage0to1ValidatorsTests
{
    [Fact]
    public void ValidateBooleanOpRequestStrict_AcceptsValidInput()
    {
        var request = new BooleanOpRequest
        {
            LeftMesh = new MeshRef { Id = "A" },
            RightMesh = new MeshRef { Id = "B" },
            Op = BooleanOperation.Union,
            Tolerances = new ToleranceBundle
            {
                DistanceEpsilon = 0.01,
                AngleEpsilonRadians = 0.001,
                AreaEpsilon = 0.1,
            },
            CoordinateSpace = CoordinateSpace.World3D,
            DeterminismPolicy = DeterminismPolicy.Strict,
        };

        ContractValidationAssert.Valid(() => Stage0to1Validators.ValidateBooleanOpRequestStrict(request));
    }

    [Fact]
    public void ValidateBooleanOpRequestStrict_RejectsMissingLeftMesh()
    {
        var request = new BooleanOpRequest
        {
            LeftMesh = null!,
            RightMesh = new MeshRef { Id = "B" },
            Op = BooleanOperation.Union,
            Tolerances = new ToleranceBundle
            {
                DistanceEpsilon = 0.01,
                AngleEpsilonRadians = 0.001,
                AreaEpsilon = 0.1,
            },
            CoordinateSpace = CoordinateSpace.World3D,
            DeterminismPolicy = DeterminismPolicy.Strict,
        };

        ContractValidationAssert.InvalidWithCode(
            () => Stage0to1Validators.ValidateBooleanOpRequestStrict(request),
            "BP01.REQUEST.LEFT_MESH_ID_INVALID");
    }

    [Fact]
    public void ValidateStage0To1ContextStrict_RejectsNonCanonicalId()
    {
        var context = new Stage0To1Context
        {
            LeftMesh = new MeshRef { Id = " A " },
            RightMesh = new MeshRef { Id = "B" },
            Op = BooleanOperation.Union,
            Tolerances = new ToleranceBundle
            {
                DistanceEpsilon = 0.01,
                AngleEpsilonRadians = 0.001,
                AreaEpsilon = 0.1,
            },
            CoordinateSpace = CoordinateSpace.World3D,
            DeterminismPolicy = DeterminismPolicy.Strict,
        };

        ContractValidationAssert.InvalidWithCode(
            () => Stage0to1Validators.ValidateStage0To1ContextStrict(context),
            "BP01.CONTEXT.LEFT_MESH_ID_NOT_CANONICAL");
    }
}

