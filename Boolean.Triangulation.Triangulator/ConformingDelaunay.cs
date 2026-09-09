using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;

namespace ConstrainedTriangulator;

/// <summary>
/// True Delaunay triangulation of a PSLG's convex hull. Missing constrained
/// edges are recovered by midpoint subdivision, not by exempting them from
/// the empty-circle criterion. Original vertices retain their indices.
/// </summary>
public static class ConformingDelaunay
{
    public static Result Run(in Input input, int maxSteinerPoints = 4096)
    {
        if (maxSteinerPoints < 0) throw new ArgumentOutOfRangeException(nameof(maxSteinerPoints));
        InputValidator.Validate(input);
        if (input.Points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)))
            throw new ArgumentException("Coordinates must be finite.");
        var points = input.Points.ToList();
        var constraints = SplitAtExistingVertices(points, input.Segments);
        int originalCount = points.Count;
        while (true)
        {
            var triangles = Triangulate(points);
            var edges = Adjacency(triangles);
            var missing = constraints.Where(e => !edges.ContainsKey(Key(e.A, e.B))).ToArray();
            if (missing.Length == 0)
            {
                Validate(points, triangles, constraints);
                return new Result(points, triangles, constraints);
            }
            if (points.Count - originalCount + missing.Length > maxSteinerPoints)
                throw new InvalidOperationException("Conforming Delaunay exceeded its Steiner vertex budget; no partial triangulation was returned.");
            foreach (var e in missing)
            {
                var a = points[e.A]; var b = points[e.B];
                var p = new RealPoint2D(a.X * .5 + b.X * .5, a.Y * .5 + b.Y * .5);
                if (points.Any(q => q.X == p.X && q.Y == p.Y))
                    throw new InvalidOperationException("A constraint cannot be refined further at double precision.");
                int id = points.Count; points.Add(p);
                constraints.Remove(e); constraints.Add((e.A, id)); constraints.Add((id, e.B));
            }
        }
    }

    private static List<(int A, int B)> SplitAtExistingVertices(IReadOnlyList<RealPoint2D> points, IReadOnlyList<(int A, int B)> segments)
    {
        var result = new HashSet<(int A, int B)>();
        foreach (var (a, b) in segments)
        {
            var p = points[a]; var q = points[b];
            bool x = Math.Abs(q.X - p.X) >= Math.Abs(q.Y - p.Y);
            double Coord(int i) => x ? points[i].X : points[i].Y;
            var on = Enumerable.Range(0, points.Count).Where(i => DelaunayPredicates.Orient(p, q, points[i]) == 0 &&
                Coord(i) >= Math.Min(Coord(a), Coord(b)) && Coord(i) <= Math.Max(Coord(a), Coord(b))).OrderBy(Coord).ToArray();
            for (int i = 1; i < on.Length; i++) result.Add(Key(on[i - 1], on[i]));
        }
        return result.OrderBy(e => e.A).ThenBy(e => e.B).ToList();
    }

    private static List<(int A, int B, int C)> Triangulate(List<RealPoint2D> p)
    {
        var order = Enumerable.Range(0, p.Count).OrderBy(i => p[i].X).ThenBy(i => p[i].Y).ToArray();
        var hull = new List<int>();
        void Append(int i, int minimum)
        {
            while (hull.Count >= minimum + 2 && DelaunayPredicates.Orient(p[hull[^2]], p[hull[^1]], p[i]) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(i);
        }
        foreach (int i in order) Append(i, 0);
        int lower = hull.Count - 1;
        for (int i = order.Length - 2; i >= 0; i--) Append(order[i], lower);
        hull.RemoveAt(hull.Count - 1);
        var triangles = new List<(int A, int B, int C)>();
        for (int i = 1; i + 1 < hull.Count; i++) triangles.Add((hull[0], hull[i], hull[i + 1]));
        var hullSet = hull.ToHashSet();
        foreach (int v in order)
        {
            if (hullSet.Contains(v)) continue;
            bool inserted = false;
            for (int ti = 0; ti < triangles.Count; ti++)
            {
                var t = triangles[ti];
                int ab = DelaunayPredicates.Orient(p[t.A], p[t.B], p[v]);
                int bc = DelaunayPredicates.Orient(p[t.B], p[t.C], p[v]);
                int ca = DelaunayPredicates.Orient(p[t.C], p[t.A], p[v]);
                if (ab < 0 || bc < 0 || ca < 0) continue;
                if (ab == 0 || bc == 0 || ca == 0)
                {
                    var edge = ab == 0 ? Key(t.A, t.B) : bc == 0 ? Key(t.B, t.C) : Key(t.C, t.A);
                    for (int j = triangles.Count - 1; j >= 0; j--)
                    {
                        var old = triangles[j]; var ids = new[] { old.A, old.B, old.C };
                        if (!ids.Contains(edge.A) || !ids.Contains(edge.B)) continue;
                        int c = ids.First(i => i != edge.A && i != edge.B);
                        triangles.RemoveAt(j);
                        triangles.Add(Ccw(edge.A, v, c, p)); triangles.Add(Ccw(v, edge.B, c, p));
                    }
                }
                else
                {
                    triangles[ti] = (t.A, t.B, v); triangles.Add((t.B, t.C, v)); triangles.Add((t.C, t.A, v));
                }
                inserted = true; break;
            }
            if (!inserted) throw new InvalidOperationException("Delaunay point insertion failed to locate a containing face.");
        }
        Legalize(p, triangles);
        return triangles;
    }

    private static void Legalize(IReadOnlyList<RealPoint2D> p, List<(int A, int B, int C)> triangles)
    {
        var visited = new HashSet<string>();
        while (true)
        {
            var state = string.Join(";", triangles.Select(t => string.Join(",", new[] { t.A, t.B, t.C }.OrderBy(i => i))).OrderBy(s => s));
            if (!visited.Add(state)) throw new InvalidOperationException("Delaunay legalization repeated a topology. Points: " + string.Join(";", p.Select(q => $"{q.X:R},{q.Y:R}")));
            bool changed = false;
            foreach (var pair in Adjacency(triangles))
            {
                if (pair.Value.Count != 2) continue;
                int i = pair.Value[0], j = pair.Value[1];
                var t = triangles[i]; var u = triangles[j];
                int c = Opposite(t, pair.Key), d = Opposite(u, pair.Key);
                int a = pair.Key.A, b = pair.Key.B;
                if (DelaunayPredicates.Orient(p[c], p[d], p[a]) * DelaunayPredicates.Orient(p[c], p[d], p[b]) >= 0) continue;
                if (DelaunayPredicates.InCircle(p[t.A], p[t.B], p[t.C], p[d]) <= 0) continue;
                triangles[i] = Ccw(c, d, a, p); triangles[j] = Ccw(d, c, b, p);
                changed = true; break;
            }
            if (!changed) return;
        }
    }

    private static (int A, int B, int C) Ccw(int a, int b, int c, IReadOnlyList<RealPoint2D> p)
    {
        int sign = DelaunayPredicates.Orient(p[a], p[b], p[c]);
        if (sign == 0) throw new InvalidOperationException("Degenerate Delaunay face.");
        return sign > 0 ? (a, b, c) : (a, c, b);
    }
    private static int Opposite((int A, int B, int C) t, (int A, int B) e) => t.A != e.A && t.A != e.B ? t.A : t.B != e.A && t.B != e.B ? t.B : t.C;
    private static (int A, int B) Key(int a, int b) => a < b ? (a, b) : (b, a);
    private static Dictionary<(int A, int B), List<int>> Adjacency(IReadOnlyList<(int A, int B, int C)> triangles)
    {
        var result = new Dictionary<(int A, int B), List<int>>();
        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];
            foreach (var e in new[] { Key(t.A, t.B), Key(t.B, t.C), Key(t.C, t.A) })
            { if (!result.TryGetValue(e, out var list)) result[e] = list = new List<int>(2); list.Add(i); }
        }
        return result;
    }
    private static void Validate(IReadOnlyList<RealPoint2D> p, List<(int A, int B, int C)> triangles, List<(int A, int B)> constraints)
    {
        var adjacency = Adjacency(triangles);
        if (constraints.Any(e => !adjacency.ContainsKey(Key(e.A, e.B)))) throw new InvalidOperationException("Missing constraint.");
        foreach (var pair in adjacency)
        {
            if (pair.Value.Count > 2) throw new InvalidOperationException("Non-manifold Delaunay edge.");
            if (pair.Value.Count != 2) continue;
            var t = triangles[pair.Value[0]]; int d = Opposite(triangles[pair.Value[1]], pair.Key);
            if (DelaunayPredicates.InCircle(p[t.A], p[t.B], p[t.C], p[d]) > 0)
                throw new InvalidOperationException("Delaunay empty-circle invariant failed.");
        }
    }
}