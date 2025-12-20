namespace BooleanPipeline.Orchestrator;

public static class BooleanPipeline
{
    public static PipelineBuilder<TIn, TIn> Start<TIn>() => new((input, _) => input);
}

