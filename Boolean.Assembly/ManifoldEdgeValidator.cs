using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Geometry;

namespace Boolean;

internal static class ManifoldEdgeValidator
{
    // Enforce manifoldness: every topological edge must be used exactly twice.
    public static void ValidateManifoldEdges(
        IReadOnlyList<RealPoint> vertices,
        IReadOnlyList<(int A, int B, int C)> triangles)
    {
        var edgeUse = new Dictionary<(int, int), int>();

        void AddEdge(int a, int b)
        {
            if (a == b) return;
            var key = a < b ? (a, b) : (b, a);
            edgeUse[key] = edgeUse.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            if (a == b || b == c || c == a)
            {
                continue;
            }

            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        var bad = new List<((int A, int B) Edge, int Count)>();
        var good = new List<(int A, int B)>();

        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 2)
            {
                bad.Add((kvp.Key, kvp.Value));
            }
            else
            {
                good.Add(kvp.Key);
            }
        }

        if (bad.Count == 0)
        {
            return;
        }

        var hist = new Dictionary<int, int>();
        for (int i = 0; i < bad.Count; i++)
        {
            int c = bad[i].Count;
            hist[c] = hist.TryGetValue(c, out int n) ? n + 1 : 1;
        }

        bad.Sort((x, y) => y.Count.CompareTo(x.Count));

        int show = Math.Min(12, bad.Count);
        var parts = new List<string>(show);

        for (int i = 0; i < show; i++)
        {
            var e = bad[i].Edge;
            var pa = vertices[e.A];
            var pb = vertices[e.B];

            parts.Add(
                $"({e.A},{e.B}) used {bad[i].Count}  " +
                $"A=({pa.X.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pa.Y.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pa.Z.ToString("G17", CultureInfo.InvariantCulture)})  " +
                $"B=({pb.X.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pb.Y.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pb.Z.ToString("G17", CultureInfo.InvariantCulture)})");
        }

        int goodShow = Math.Min(12, good.Count);
        var goodParts = new List<string>(goodShow);
        for (int i = 0; i < goodShow; i++)
        {
            var e = good[i];
            var pa = vertices[e.A];
            var pb = vertices[e.B];
            goodParts.Add(
                $"({e.A},{e.B})  " +
                $"A=({pa.X.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pa.Y.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pa.Z.ToString("G17", CultureInfo.InvariantCulture)})  " +
                $"B=({pb.X.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pb.Y.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pb.Z.ToString("G17", CultureInfo.InvariantCulture)})");
        }

        string histStr = string.Join(", ", hist.OrderBy(k => k.Key).Select(k => $"{k.Key}ƒ+'{k.Value}"));

        throw new InvalidOperationException(
            $"Non-manifold edges detected in boolean mesh assembly ({bad.Count}). " +
            $"Edge-use histogram: {histStr}. " +
            $"Top edges: {string.Join(" | ", parts)}. " +
            $"Good edges: {good.Count} (sample {goodShow}): {string.Join(" | ", goodParts)}");
    }
}
