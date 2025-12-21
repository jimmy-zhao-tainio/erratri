using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Geometry;

namespace Kernel;

internal static class EdgeUseDebug
{
    public static void Print(
        string tag,
        IReadOnlyList<RealPoint> verts,
        IReadOnlyList<(int A, int B, int C)> tris,
        int top = 10,
        IReadOnlyList<char>? triangleSources = null)
    {
        var use = new Dictionary<(int, int), int>();
        var firstTri = new Dictionary<(int, int), int>();

        void AddEdge(int a, int b, int triIndex)
        {
            if (a == b) return;
            var key = a < b ? (a, b) : (b, a);
            use[key] = use.TryGetValue(key, out var c) ? c + 1 : 1;
            if (!firstTri.ContainsKey(key))
            {
                firstTri[key] = triIndex;
            }
        }

        for (int i = 0; i < tris.Count; i++)
        {
            var (a, b, c) = tris[i];
            if (a == b || b == c || c == a) continue;
            AddEdge(a, b, i);
            AddEdge(b, c, i);
            AddEdge(c, a, i);
        }

        int c1 = 0, c2 = 0, c3 = 0, c4p = 0;
        foreach (var kv in use)
        {
            if (kv.Value == 1) c1++;
            else if (kv.Value == 2) c2++;
            else if (kv.Value == 3) c3++;
            else c4p++;
        }

        Console.WriteLine($"[{tag}] V={verts.Count} T={tris.Count} edges: 1->{c1}, 2->{c2}, 3->{c3}, 4+->{c4p}");

        if (c1 == 0) return;

        var bad = new List<((int, int) E, int C)>();
        foreach (var kv in use)
        {
            if (kv.Value != 2) bad.Add((kv.Key, kv.Value));
        }

        bad.Sort((x, y) =>
        {
            int rx = x.C == 1 ? 0 : (x.C == 3 ? 1 : 2);
            int ry = y.C == 1 ? 0 : (y.C == 3 ? 1 : 2);
            int cmp = rx.CompareTo(ry);
            if (cmp != 0) return cmp;
            return y.C.CompareTo(x.C);
        });

        int n = Math.Min(top, bad.Count);
        for (int i = 0; i < n; i++)
        {
            var (e, cnt) = bad[i];
            var a = verts[e.Item1];
            var b = verts[e.Item2];

            string extra = string.Empty;
            if (cnt == 1 &&
                triangleSources is not null &&
                firstTri.TryGetValue(e, out int triIdx) &&
                triIdx >= 0 &&
                triIdx < tris.Count &&
                triIdx < triangleSources.Count &&
                string.Equals(Environment.GetEnvironmentVariable("ERRATRI_DEBUG_STAGE2"), "1", StringComparison.Ordinal))
            {
                var t = tris[triIdx];
                char src = triangleSources[triIdx];
                extra = $" tri#{triIdx}=({t.A},{t.B},{t.C}) src={src}";
            }

            Console.WriteLine($"  edge ({e.Item1},{e.Item2}) used {cnt}  A=({a.X},{a.Y},{a.Z})  B=({b.X},{b.Y},{b.Z}){extra}");
        }
    }
}

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

        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 2)
            {
                bad.Add((kvp.Key, kvp.Value));
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

        string histStr = string.Join(", ", hist.OrderBy(k => k.Key).Select(k => $"{k.Key}→{k.Value}"));

        if (string.Equals(Environment.GetEnvironmentVariable("ERRATRI_DEBUG_STAGE2"), "1", StringComparison.Ordinal))
        {
            EdgeUseDebug.Print("validate", vertices, triangles, top: 10);
        }

        throw new InvalidOperationException(
            $"Non-manifold edges detected in boolean mesh assembly ({bad.Count}). " +
            $"Edge-use histogram: {histStr}. " +
            $"Top edges: {string.Join(" | ", parts)}");
    }
}
