namespace Contracts.Core;

/// <summary>
/// Helpers for deterministic ordering and stable iteration.
/// </summary>
/// <remarks>
/// This is a utility type. Contract-level determinism requirements are expressed via policy values
/// (e.g., <c>Contracts.Stage0to1.DeterminismPolicy</c>) and enforced by validators.
/// </remarks>
public static class Determinism
{
    public static readonly StringComparer StableStringComparer = StringComparer.Ordinal;

    public static IReadOnlyList<T> OrderByStable<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> keySelector,
        IComparer<TKey>? comparer = null)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

        comparer ??= Comparer<TKey>.Default;
        return items.OrderBy(keySelector, comparer).ToArray();
    }
}
