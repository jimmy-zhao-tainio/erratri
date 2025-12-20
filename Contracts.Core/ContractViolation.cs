namespace Contracts.Core;

public sealed record ContractViolation(ContractErrorCode Code, string Message, string? Path = null);

