namespace Contracts.Core;

public static class ContractGuard
{
    public static TOut RunStrict<TIn, TOut>(
        TIn input,
        Action<TIn> validateInputStrict,
        Func<TIn, TOut> run,
        Action<TOut> validateOutputStrict)
    {
        if (validateInputStrict is null) throw new ArgumentNullException(nameof(validateInputStrict));
        if (run is null) throw new ArgumentNullException(nameof(run));
        if (validateOutputStrict is null) throw new ArgumentNullException(nameof(validateOutputStrict));

        validateInputStrict(input);
        var output = run(input);
        validateOutputStrict(output);
        return output;
    }
}

