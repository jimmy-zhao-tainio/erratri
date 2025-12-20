using Contracts.Core;
using Contracts.Stage0to1;
using Contracts.Stage1to2;
using Contracts.Testing;

namespace Contracts.Stage1to2.Tests;

public sealed class Stage1to2ValidatorsTests
{
    [Fact]
    public void ValidateStage1To2ContextStrict_AcceptsValidContext()
    {
        var context = new Stage1To2Context
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

        ContractValidationAssert.Valid(() => Stage1to2Validators.ValidateStage1To2ContextStrict(context));
    }

    [Fact]
    public void ValidateStage1To2ContextStrict_RejectsNonCanonicalId()
    {
        var context = new Stage1To2Context
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
            () => Stage1to2Validators.ValidateStage1To2ContextStrict(context),
            "BP02.CONTEXT.LEFT_MESH_ID_NOT_CANONICAL");
    }
}

