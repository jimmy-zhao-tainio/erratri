using BooleanPipeline.Orchestrator;
using Contracts.Stage0to1;

namespace BooleanPipeline.Stage0;

public static class Stage0Stage
{
    public static Stage<BooleanOpRequest, Stage0To1Context> Create() =>
        new(
            Name: "Stage0",
            Execute: (request, _) => Stage0.Run(request),
            ValidateInputStrict: Stage0to1Validators.ValidateBooleanOpRequestStrict,
            ValidateOutputStrict: Stage0to1Validators.ValidateStage0To1ContextStrict);
}

