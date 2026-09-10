using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;

namespace ConstrainedTriangulator;

/// <summary>Conforming Delaunay on a planar integer lattice, with exact predicates.
/// Refinement never moves an input point or leaves its integer source segment.
/// An unrecoverable primitive segment or exhausted budget throws, without a partial result.</summary>
public sealed class IntegerConformingDelaunay
{
    private readonly IntegerPlane plane;
    private IntegerConformingDelaunay(IntegerPlane plane) { this.plane = plane; }

    public sealed record GridResult(IReadOnlyList<Point> Points,
        IReadOnlyList<(int A, int B, int C)> Triangles, IReadOnlyList<(int A, int B)> Segments);

    public static GridResult Run(IReadOnlyList<Point> points,
        IReadOnlyList<(int A, int B)> segments, int maxSteinerPoints = 4096)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(segments);
        if (maxSteinerPoints < 0) throw new ArgumentOutOfRangeException(nameof(maxSteinerPoints));
        if (points.Count < 3 || points.Distinct().Count() != points.Count)
            throw new ArgumentException("At least three distinct integer points are required.");
        IntegerPlane? plane = null;
        for (int i = 2; i < points.Count && plane is null; i++)
        {
            if (!IntegerPlane.AreCollinear(points[0], points[1], points[i]))
                plane = new IntegerPlane(points[0], points[1], points[i]);
        }
        if (plane is null || points.Any(p => !plane.Contains(p)))
            throw new ArgumentException("Points must span one plane.");
        foreach (var (a, b) in segments)
            if (a < 0 || b < 0 || a >= points.Count || b >= points.Count || a == b)
                throw new ArgumentException("Invalid segment endpoints.");
        var algorithm = new IntegerConformingDelaunay(plane);
        var constraints = algorithm.SplitAtExistingVertices(points, segments);
        for (int i = 0; i < constraints.Count; i++)
            for (int j = i + 1; j < constraints.Count; j++)
            {
                var (a, b) = constraints[i]; var (c, d) = constraints[j];
                if (plane.Orient(points[a], points[b], points[c]) * plane.Orient(points[a], points[b], points[d]) < 0 &&
                    plane.Orient(points[c], points[d], points[a]) * plane.Orient(points[c], points[d], points[b]) < 0)
                    throw new ArgumentException("Crossing constraints must be split at a supplied integer vertex.");
            }
        return algorithm.Refine(points.ToList(), constraints, maxSteinerPoints);
    }

    private GridResult Refine(List<Point> points, List<(int A, int B)> constraints, int budget)
    {
        int originalCount = points.Count;
        while (true)
        {
            var triangles = Triangulate(points);
            var edges = Adjacency(triangles);
            var missing = constraints.Where(e => !edges.ContainsKey(Key(e.A, e.B))).ToArray();
            if (missing.Length == 0)
            {
                Validate(points, triangles, constraints);
                return new GridResult(points.AsReadOnly(), triangles.AsReadOnly(), constraints.AsReadOnly());
            }
            // A cocircular polygon admits several valid diagonals. Recover a
            // requested one by removing crossing tie edges before adding points.
            bool Crosses((int A, int B) e, (int A, int B) f) =>
                plane.Orient(points[e.A], points[e.B], points[f.A]) * plane.Orient(points[e.A], points[e.B], points[f.B]) < 0 &&
                plane.Orient(points[f.A], points[f.B], points[e.A]) * plane.Orient(points[f.A], points[f.B], points[e.B]) < 0;
            foreach (var e in missing)
            {
                while (!edges.ContainsKey(e))
                {
                    bool changed = false;
                    foreach (var adjacent in edges)
                    {
                        if (adjacent.Value.Count != 2 || constraints.Contains(adjacent.Key) || !Crosses(e, adjacent.Key)) continue;
                        int i = adjacent.Value[0], j = adjacent.Value[1];
                        int c = Opposite(triangles[i], adjacent.Key), d = Opposite(triangles[j], adjacent.Key);
                        if (Crosses(e, Key(c, d)) || !Crosses(adjacent.Key, Key(c, d))) continue;
                        var t = triangles[i];
                        if (plane.InCircle(points[t.A], points[t.B], points[t.C], points[d]) != 0) continue;
                        triangles[i] = Ccw(c, d, adjacent.Key.A, points);
                        triangles[j] = Ccw(d, c, adjacent.Key.B, points);
                        edges = Adjacency(triangles);
                        changed = true;
                        break;
                    }
                    if (!changed) break;
                }
            }
            missing = constraints.Where(e => !edges.ContainsKey(e)).ToArray();
            if (missing.Length == 0)
            {
                Validate(points, triangles, constraints);
                return new GridResult(points.AsReadOnly(), triangles.AsReadOnly(), constraints.AsReadOnly());
            }
            foreach (var e in missing)
            {
                if (!IntegerPlane.TrySplit(points[e.A], points[e.B], out var p))
                    throw new InvalidOperationException($"Constraint ({e.A}, {e.B}) is absent from this Delaunay triangulation and has no interior lattice point; integer refinement cannot recover it.");
                if (points.Count - originalCount >= budget)
                    throw new InvalidOperationException("Integer conforming Delaunay exceeded its Steiner vertex budget; no partial result was returned.");
                int id = points.Count; points.Add(p);
                constraints.Remove(e); constraints.Add(Key(e.A, id)); constraints.Add(Key(id, e.B));
            }
        }
    }

    private List<(int A, int B)> SplitAtExistingVertices(IReadOnlyList<Point> points, IReadOnlyList<(int A, int B)> segments)
    {
        var result = new HashSet<(int A, int B)>();
        foreach (var (a, b) in segments)
        {
            var on = Enumerable.Range(0, points.Count)
                .Where(i => IntegerPlane.OnSegment(points[a], points[b], points[i]))
                .OrderBy(i => points[i], Comparer<Point>.Create(plane.Compare)).ToArray();
            for (int i = 1; i < on.Length; i++) result.Add(Key(on[i - 1], on[i]));
        }
        return result.OrderBy(e => e.A).ThenBy(e => e.B).ToList();
    }

    private List<(int A, int B, int C)> Triangulate(List<Point> p)
    {
        var order = Enumerable.Range(0, p.Count).OrderBy(i => p[i], Comparer<Point>.Create(plane.Compare)).ToArray();
        var hull = new List<int>();
        void Append(int i, int minimum)
        {
            while (hull.Count >= minimum + 2 && plane.Orient(p[hull[^2]], p[hull[^1]], p[i]) <= 0) hull.RemoveAt(hull.Count - 1);
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
                int ab = plane.Orient(p[t.A], p[t.B], p[v]);
                int bc = plane.Orient(p[t.B], p[t.C], p[v]);
                int ca = plane.Orient(p[t.C], p[t.A], p[v]);
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

    private void Legalize(IReadOnlyList<Point> p, List<(int A, int B, int C)> triangles)
    {
        while (true)
        {
            bool changed = false;
            foreach (var pair in Adjacency(triangles))
            {
                if (pair.Value.Count != 2) continue;
                int i = pair.Value[0], j = pair.Value[1];
                var t = triangles[i]; var u = triangles[j];
                int c = Opposite(t, pair.Key), d = Opposite(u, pair.Key);
                int a = pair.Key.A, b = pair.Key.B;
                if (plane.Orient(p[c], p[d], p[a]) * plane.Orient(p[c], p[d], p[b]) >= 0) continue;
                if (plane.InCircle(p[t.A], p[t.B], p[t.C], p[d]) <= 0) continue;
                triangles[i] = Ccw(c, d, a, p); triangles[j] = Ccw(d, c, b, p);
                changed = true; break;
            }
            if (!changed) return;
        }
    }

    private (int A, int B, int C) Ccw(int a, int b, int c, IReadOnlyList<Point> p)
    {
        int sign = plane.Orient(p[a], p[b], p[c]);
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
    private void Validate(IReadOnlyList<Point> p, List<(int A, int B, int C)> triangles, List<(int A, int B)> constraints)
    {
        var adjacency = Adjacency(triangles);
        if (constraints.Any(e => !adjacency.ContainsKey(Key(e.A, e.B)))) throw new InvalidOperationException("Missing constraint.");
        foreach (var pair in adjacency)
        {
            if (pair.Value.Count > 2) throw new InvalidOperationException("Non-manifold Delaunay edge.");
            if (pair.Value.Count != 2) continue;
            var t = triangles[pair.Value[0]]; int d = Opposite(triangles[pair.Value[1]], pair.Key);
            if (plane.InCircle(p[t.A], p[t.B], p[t.C], p[d]) > 0)
                throw new InvalidOperationException("Delaunay empty-circle invariant failed.");
        }
    }
}