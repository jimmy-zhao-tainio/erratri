using System.Collections.Generic;

namespace Kernel;

internal static class TriangleCleanup
{
    // Removes collapsed tris and dedupes triangles by vertex set (ignoring winding).
    public static void DeduplicateIgnoringWindingInPlace(List<(int A, int B, int C)> triangles)
    {
        if (triangles.Count == 0)
        {
            return;
        }

        var deduped = new List<(int A, int B, int C)>(triangles.Count);
        var seen = new HashSet<(int Min, int Mid, int Max)>();

        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];

            if (t.A == t.B || t.B == t.C || t.C == t.A)
            {
                continue;
            }

            int min = t.A, mid = t.B, max = t.C;

            if (min > mid) (min, mid) = (mid, min);
            if (mid > max) (mid, max) = (max, mid);
            if (min > mid) (min, mid) = (mid, min);

            if (seen.Add((min, mid, max)))
            {
                deduped.Add(t);
            }
        }

        triangles.Clear();
        triangles.AddRange(deduped);
    }

    public static int DeduplicateIgnoringWindingInPlace(List<(int A, int B, int C)> triangles, List<string> provenance)
    {
        if (provenance is null) throw new System.ArgumentNullException(nameof(provenance));
        if (provenance.Count != triangles.Count)
        {
            throw new System.InvalidOperationException($"Provenance list must match triangle count (prov={provenance.Count}, tris={triangles.Count}).");
        }

        if (triangles.Count == 0)
        {
            return 0;
        }

        var deduped = new List<(int A, int B, int C)>(triangles.Count);
        var dedupedProv = new List<string>(provenance.Count);
        var seen = new HashSet<(int Min, int Mid, int Max)>();
        int removed = 0;

        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];

            if (t.A == t.B || t.B == t.C || t.C == t.A)
            {
                removed++;
                continue;
            }

            int min = t.A, mid = t.B, max = t.C;

            if (min > mid) (min, mid) = (mid, min);
            if (mid > max) (mid, max) = (max, mid);
            if (min > mid) (min, mid) = (mid, min);

            if (seen.Add((min, mid, max)))
            {
                deduped.Add(t);
                dedupedProv.Add(provenance[i]);
            }
            else
            {
                removed++;
            }
        }

        triangles.Clear();
        triangles.AddRange(deduped);

        provenance.Clear();
        provenance.AddRange(dedupedProv);

        return removed;
    }
}
