namespace Contracts.Core;

public sealed class ContractValidationException : Exception
{
    public IReadOnlyList<ContractViolation> Violations { get; }

    public ContractValidationException(IReadOnlyList<ContractViolation> violations)
        : base(BuildMessage(violations))
    {
        Violations = violations;
    }

    private static string BuildMessage(IReadOnlyList<ContractViolation> violations)
    {
        if (violations.Count == 0)
            return "Contract validation failed.";

        var first = violations[0];
        return first.Path is null
            ? $"Contract validation failed: [{first.Code}] {first.Message}"
            : $"Contract validation failed: [{first.Code}] {first.Path}: {first.Message}";
    }
}

