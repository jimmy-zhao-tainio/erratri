using Contracts.Core;

namespace BooleanPipeline.Orchestrator;

public sealed class PipelineBuilder<TIn, TCurrent>
{
    private readonly Func<TIn, CancellationToken, TCurrent> _run;

    internal PipelineBuilder(Func<TIn, CancellationToken, TCurrent> run)
    {
        _run = run;
    }

    public PipelineBuilder<TIn, TNext> Then<TNext>(Stage<TCurrent, TNext> stage)
    {
        if (stage is null) throw new ArgumentNullException(nameof(stage));

        return new PipelineBuilder<TIn, TNext>((input, ct) =>
        {
            var current = _run(input, ct);
            return ContractGuard.RunStrict(
                current,
                stage.ValidateInputStrict,
                v => stage.Execute(v, ct),
                stage.ValidateOutputStrict);
        });
    }

    public TCurrent Run(TIn input, CancellationToken ct = default) => _run(input, ct);
}

