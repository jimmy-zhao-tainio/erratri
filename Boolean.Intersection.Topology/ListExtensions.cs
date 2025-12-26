using System;
using System.Collections.Generic;

namespace Boolean;

internal static class ListExtensions
{
    public static double Median(this List<double> values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (values.Count == 0) return 0.0;

        values.Sort();
        int n = values.Count;
        int mid = n / 2;

        if ((n & 1) == 1)
        {
            return values[mid];
        }

        return 0.5 * (values[mid - 1] + values[mid]);
    }
}

