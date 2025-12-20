using BooleanPipeline.Orchestrator;
using Contracts.Stage0to1;
using Contracts.Stage1to2;

namespace BooleanPipeline.Stage1;

public static class Stage1Stage
{
    public static Stage<Stage0To1Context, Stage1To2Context> Create() =>
        new(
            Name: "Stage1",
            Execute: (context, _) => Stage1.Run(context),
            ValidateInputStrict: Stage0to1Validators.ValidateStage0To1ContextStrict,
            ValidateOutputStrict: Stage1to2Validators.ValidateStage1To2ContextStrict);
}

