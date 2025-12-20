using BooleanPipeline.Orchestrator;
using Contracts.Core;
using Contracts.Testing;

namespace BooleanPipeline.Orchestrator.Tests;

public sealed class UnitTest1
{
    [Fact]
    public void Then_EnforcesPreAndPostValidation()
    {
        var calls = new List<string>();

        var stage = new Stage<int, int>(
            Name: "AddOne",
            Execute: (v, _) =>
            {
                calls.Add("execute");
                return v + 1;
            },
            ValidateInputStrict: v =>
            {
                calls.Add("validate-in");
                if (v < 0)
                {
                    var ctx = new ContractValidationContext();
                    ctx.Add(ContractErrorCode.From("BP00.ORCHESTRATOR.NEGATIVE_INPUT"), "Value must be >= 0", "v");
                    ctx.ThrowIfAny();
                }
            },
            ValidateOutputStrict: v =>
            {
                calls.Add("validate-out");
                if (v == 0)
                {
                    var ctx = new ContractValidationContext();
                    ctx.Add(ContractErrorCode.From("BP00.ORCHESTRATOR.ZERO_OUTPUT"), "Output must not be 0", "v");
                    ctx.ThrowIfAny();
                }
            });

        var pipeline = BooleanPipeline.Start<int>().Then(stage);
        var result = pipeline.Run(41);

        Assert.Equal(42, result);
        Assert.Equal(new[] { "validate-in", "execute", "validate-out" }, calls);

        calls.Clear();
        ContractValidationAssert.InvalidWithCode(() => pipeline.Run(-1), "BP00.ORCHESTRATOR.NEGATIVE_INPUT");
        Assert.Equal(new[] { "validate-in" }, calls);
    }
}
