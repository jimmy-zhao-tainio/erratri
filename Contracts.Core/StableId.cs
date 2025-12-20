namespace Contracts.Core;

/// <summary>
/// Typed wrapper over <see cref="int"/> for IDs that may be considered “stable” by a specific contract.
/// </summary>
/// <remarks>
/// This type provides no stability guarantees across runs by itself. Any stability guarantees must be defined
/// per-contract, including deterministic assignment rules and/or an emitted mapping when IDs are reassigned.
/// </remarks>
public readonly record struct StableId(int Value)
{
    public override string ToString() => Value.ToString();
}
