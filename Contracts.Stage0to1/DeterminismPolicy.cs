namespace Contracts.Stage0to1;

/// <summary>
/// Stage0 policy for determinism expectations across the BooleanPipeline contract boundary.
/// </summary>
/// <remarks>
/// This is a policy value carried in Stage0 contracts. It is distinct from
/// <c>Contracts.Core.Determinism</c>, which provides helper utilities for stable ordering.
/// </remarks>
public enum DeterminismPolicy
{
    Strict = 0,
    Relaxed = 1,
}
