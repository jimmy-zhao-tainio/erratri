using BooleanPipeline.Orchestrator;
using Contracts.Core;
using Contracts.Stage0to1;
using Contracts.Testing;

namespace BooleanPipeline.Stage0.Tests;

public sealed class Stage0Tests
{
    [Fact]
    public void Run_TrimsMeshIds()
    {
        var request = new BooleanOpRequest
        {
            LeftMesh = new MeshRef { Id = " A " },
            RightMesh = new MeshRef { Id = " B " },
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

        var output = Stage0.Run(request);

        Assert.Equal("A", output.LeftMesh.Id);
        Assert.Equal("B", output.RightMesh.Id);
    }

    [Fact]
    public void OrchestratorStage_EnforcesValidation()
    {
        var request = new BooleanOpRequest
        {
            LeftMesh = new MeshRef { Id = " " },
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

        var pipeline = BooleanPipeline.Orchestrator.BooleanPipeline.Start<BooleanOpRequest>()
            .Then(Stage0Stage.Create());

        ContractValidationAssert.InvalidWithCode(
            () => pipeline.Run(request),
            "BP01.REQUEST.LEFT_MESH_ID_INVALID");
    }
}

