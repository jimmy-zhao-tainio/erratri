using System.Collections.ObjectModel;

namespace Contracts.Core;

public sealed class ContractValidationContext
{
    private readonly List<ContractViolation> _violations = new();

    public IReadOnlyList<ContractViolation> Violations => new ReadOnlyCollection<ContractViolation>(_violations);

    public void Add(ContractErrorCode code, string message, string? path = null) =>
        _violations.Add(new ContractViolation(code, message, path));

    public void Require(bool condition, ContractErrorCode code, string message, string? path = null)
    {
        if (!condition) Add(code, message, path);
    }

    public void ThrowIfAny()
    {
        if (_violations.Count == 0) return;
        throw new ContractValidationException(Violations);
    }
}

