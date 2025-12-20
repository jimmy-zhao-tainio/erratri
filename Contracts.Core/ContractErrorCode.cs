namespace Contracts.Core;

/// <summary>
/// Stable, semantic, hierarchical error code used as the primary key for contract validation failures.
/// </summary>
/// <remarks>
/// Canonical format:
/// <list type="bullet">
/// <item><description><c>CORE.&lt;DOMAIN&gt;.&lt;DETAIL&gt;</c> for <c>Contracts.Core</c> codes</description></item>
/// <item><description><c>BP##.&lt;DOMAIN&gt;.&lt;DETAIL&gt;</c> for stage/contract codes</description></item>
/// </list>
/// All segments are uppercase and dot-separated; underscores are allowed for readability (typically in the detail).
/// Example: <c>CORE.TOLERANCE.DISTANCE_EPSILON_INVALID</c>, <c>BP05.INTERSECTION_GRAPH.EDGE_MULTIPLICITY_GT_2</c>.
/// </remarks>
public readonly record struct ContractErrorCode
{
    public string Value { get; }

    public ContractErrorCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Error code must be non-empty.", nameof(value));

        if (!IsValid(value))
            throw new ArgumentException($"Error code must match canonical format (got: '{value}').", nameof(value));

        Value = value;
    }

    public static ContractErrorCode From(string value) => new(value);

    public override string ToString() => Value;

    private static bool IsValid(string value)
    {
        // Require at least: PREFIX.DOMAIN.DETAIL
        // Allow additional dot-separated domain segments; allow underscores/digits in segments.
        return System.Text.RegularExpressions.Regex.IsMatch(
            value,
            @"^(CORE|BP\d\d)\.[A-Z0-9_]+\.[A-Z0-9_]+(\.[A-Z0-9_]+)*$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }
}
