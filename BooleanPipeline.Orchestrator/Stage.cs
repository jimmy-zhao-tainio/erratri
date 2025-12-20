namespace BooleanPipeline.Orchestrator;

public sealed record Stage<TIn, TOut>(
    string Name,
    Func<TIn, CancellationToken, TOut> Execute,
    Action<TIn> ValidateInputStrict,
    Action<TOut> ValidateOutputStrict);

