using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Geometry;

namespace Boolean;

internal static class EdgeUseDiagnostics
{
    internal static bool DebugStage2Enabled
        => string.Equals(Environment.GetEnvironmentVariable("ERRATRI_DEBUG_STAGE2"), "1", StringComparison.Ordinal);

    public static void PrintEdgeUseFromIndexed(
        string label,
        IReadOnlyList<RealPoint> vertices,
        IReadOnlyList<(int A, int B, int C)> triangles,
        IReadOnlyList<string>? provenance = null,
        int maxEdges = 10)
    {
        var edgeToTris = new Dictionary<(int Min, int Max), List<int>>();
        void AddEdge(int triIndex, int u, int v)
        {
            if (u == v) return;
            var key = u < v ? (u, v) : (v, u);
            if (!edgeToTris.TryGetValue(key, out var list))
            {
                list = new List<int>(2);
                edgeToTris[key] = list;
            }
            list.Add(triIndex);
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            if (a == b || b == c || c == a) continue;
            AddEdge(i, a, b);
            AddEdge(i, b, c);
            AddEdge(i, c, a);
        }

        int c1 = 0, c2 = 0, c3 = 0, c4p = 0;
        var bad = new List<((int Min, int Max) Edge, int Count)>();

        foreach (var kvp in edgeToTris)
        {
            int count = kvp.Value.Count;
            if (count == 1) c1++;
            else if (count == 2) c2++;
            else if (count == 3) c3++;
            else c4p++;

            if (count != 2)
            {
                bad.Add((kvp.Key, count));
            }
        }

        Console.WriteLine($"[{label}] edge-use counts: 1→{c1}, 2→{c2}, 3→{c3}, 4+→{c4p} (triangles={triangles.Count})");

        if (bad.Count == 0)
        {
            return;
        }

        bad.Sort((x, y) => y.Count.CompareTo(x.Count));
        int show = Math.Min(maxEdges, bad.Count);

        for (int i = 0; i < show; i++)
        {
            var e = bad[i].Edge;
            int count = bad[i].Count;
            var pa = vertices[e.Min];
            var pb = vertices[e.Max];

            string pos =
                $"A=({pa.X.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pa.Y.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pa.Z.ToString("G17", CultureInfo.InvariantCulture)}) " +
                $"B=({pb.X.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pb.Y.ToString("G17", CultureInfo.InvariantCulture)}," +
                $"{pb.Z.ToString("G17", CultureInfo.InvariantCulture)})";

            string triInfo = string.Empty;
            if (edgeToTris.TryGetValue(e, out var tris))
            {
                var incident = tris.Take(3).Select(ti =>
                {
                    var t = triangles[ti];
                    string prov = provenance is null || ti < 0 || ti >= provenance.Count ? "" : $" prov={provenance[ti]}";
                    return $"tri#{ti}=({t.A},{t.B},{t.C}){prov}";
                });
                triInfo = " " + string.Join(" ", incident);
            }

            Console.WriteLine($"  edge({e.Min},{e.Max}) used {count} {pos}{triInfo}");
        }
    }

    public static void PrintEdgeUseFromRealTriangles(
        string label,
        IReadOnlyList<RealTriangle> triangles,
        IReadOnlyList<string>? provenance = null)
    {
        var vertices = new List<RealPoint>();
        var indexed = new List<(int A, int B, int C)>(triangles.Count);
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();

        for (int i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            if (RealTriangle.HasZeroArea(tri.P0, tri.P1, tri.P2))
            {
                continue;
            }

            int i0 = VertexQuantizer.AddOrGet(vertices, vertexMap, tri.P0);
            int i1 = VertexQuantizer.AddOrGet(vertices, vertexMap, tri.P1);
            int i2 = VertexQuantizer.AddOrGet(vertices, vertexMap, tri.P2);

            if (i0 == i1 || i1 == i2 || i2 == i0)
            {
                continue;
            }

            indexed.Add((i0, i1, i2));
        }

        VertexWelder.WeldInPlace(vertices, indexed, Tolerances.MergeEpsilon);
        TriangleCleanup.DeduplicateIgnoringWindingInPlace(indexed);

        PrintEdgeUseFromIndexed(label, vertices, indexed, provenance: null);
    }
}

