namespace Contracts.Core;

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

