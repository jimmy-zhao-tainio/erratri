using Geometry;

namespace Geometry.Topology;

/// <summary>Exact legalization of coplanar interior edges in an integer surface.
/// Boundaries, creases, and edges whose flip would duplicate an existing edge
/// are constraints. Requires nondegenerate, consistently oriented manifold input.
/// No vertex is added or moved.</summary>
public static class IntegerMeshDelaunay
{
    public static void ConformAndLegalize(IReadOnlyList<Point> points, List<(int A, int B, int C)> triangles)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(triangles);
        Conform(points, triangles);
        var edges = Adjacency(triangles);
        var queue = new Queue<(int A, int B)>(edges.Keys);
        while (true)
        {
            if (!queue.TryDequeue(out var edge))
            {
                // A formerly blocked flip may become possible after a distant
                // use of its alternate diagonal disappears.
                if (CountFlippableIllegalEdges(points, triangles) == 0) break;
                foreach (var candidate in edges.Keys) queue.Enqueue(candidate);
                continue;
            }
            if (!edges.TryGetValue(edge, out var uses) || uses.Count != 2) continue;
            var pair = uses.ToArray();
            int i = pair[0], j = pair[1];
            if (!TryFlip(points, triangles[i], triangles[j], edge, out var left, out var right)) continue;
            var diagonal = Key(left.A, left.B);
            if (edges.ContainsKey(diagonal)) continue; // Surface link condition.
            foreach (int face in pair)
                foreach (var old in Edges(triangles[face]))
                {
                    edges[old].Remove(face);
                    if (edges[old].Count == 0) edges.Remove(old);
                }
            triangles[i] = left; triangles[j] = right;
            foreach (int face in pair)
                foreach (var added in Edges(triangles[face]))
                {
                    if (!edges.TryGetValue(added, out var faces)) edges[added] = faces = new();
                    faces.Add(face); queue.Enqueue(added);
                }
        }
    }

    public static int CountFlippableIllegalEdges(IReadOnlyList<Point> points, IReadOnlyList<(int A, int B, int C)> triangles)
    {
        int count = 0;
        var adjacency = Adjacency(triangles);
        foreach (var edge in adjacency)
        {
            if (edge.Value.Count != 2) continue;
            var pair = edge.Value.ToArray();
            if (TryFlip(points, triangles[pair[0]], triangles[pair[1]], edge.Key, out var left, out _) && !adjacency.ContainsKey(Key(left.A, left.B))) count++;
        }
        return count;
    }

    private static bool TryFlip(IReadOnlyList<Point> p, (int A, int B, int C) t, (int A, int B, int C) u,
        (int A, int B) edge, out (int A, int B, int C) left, out (int A, int B, int C) right)
    {
        left = right = default;
        int a = edge.A, b = edge.B, c = Opposite(t, edge), d = Opposite(u, edge);
        if (c == d) return false;
        var plane = new IntegerPlane(p[t.A], p[t.B], p[t.C]);
        if (!plane.Contains(p[d])) return false; // Preserve the exact piecewise-planar surface.
        int winding = plane.Orient(p[t.A], p[t.B], p[t.C]);
        if (plane.Orient(p[u.A], p[u.B], p[u.C]) != winding ||
            plane.Orient(p[a], p[b], p[c]) * plane.Orient(p[a], p[b], p[d]) >= 0 ||
            plane.Orient(p[c], p[d], p[a]) * plane.Orient(p[c], p[d], p[b]) >= 0 ||
            plane.InCircle(p[t.A], p[t.B], p[t.C], p[d]) <= 0) return false;
        if (plane.Orient(p[c], p[d], p[a]) != winding) (c, d) = (d, c);
        left = (c, d, a); right = (d, c, b);
        return true;
    }

    private static void Conform(IReadOnlyList<Point> points, List<(int A, int B, int C)> triangles)
    {
        var chains = new Dictionary<(int A, int B), int[]>();
        int[] Chain(int a, int b)
        {
            var key = Key(a, b);
            if (!chains.TryGetValue(key, out var chain))
            {
                var p = points[key.A]; var q = points[key.B];
                long Coordinate(int i) => p.X != q.X ? points[i].X : p.Y != q.Y ? points[i].Y : points[i].Z;
                chain = Enumerable.Range(0, points.Count).Where(i => IntegerPlane.OnSegment(p, q, points[i]))
                    .OrderBy(Coordinate).ToArray();
                chains.Add(key, chain);
            }
            return chain;
        }
        var output = new List<(int A, int B, int C)>();
        foreach (var t in triangles)
        {
            var ring = new List<int>();
            foreach (var (a, b) in new[] { (t.A, t.B), (t.B, t.C), (t.C, t.A) })
            {
                var chain = Chain(a, b);
                if (chain[0] == a) ring.AddRange(chain.Take(chain.Length - 1));
                else for (int i = chain.Length - 1; i > 0; i--) ring.Add(chain[i]);
            }
            var plane = new IntegerPlane(points[t.A], points[t.B], points[t.C]);
            int winding = plane.Orient(points[t.A], points[t.B], points[t.C]);
            int Side(int a, int b, int c) => winding * plane.Orient(points[a], points[b], points[c]);
            while (ring.Count > 3)
            {
                int ear = -1;
                for (int i = 0; i < ring.Count; i++)
                {
                    int a = ring[(i + ring.Count - 1) % ring.Count], b = ring[i], c = ring[(i + 1) % ring.Count];
                    if (Side(a, b, c) <= 0 || ring.Any(p => p != a && p != b && p != c &&
                        Side(a, b, p) >= 0 && Side(b, c, p) >= 0 && Side(c, a, p) >= 0)) continue;
                    ear = i; break;
                }
                if (ear < 0) throw new InvalidOperationException("Cannot conform the integer triangle boundary.");
                output.Add((ring[(ear + ring.Count - 1) % ring.Count], ring[ear], ring[(ear + 1) % ring.Count]));
                ring.RemoveAt(ear);
            }
            output.Add((ring[0], ring[1], ring[2]));
        }
        triangles.Clear(); triangles.AddRange(output);
    }

    private static int Opposite((int A, int B, int C) t, (int A, int B) e) =>
        t.A != e.A && t.A != e.B ? t.A : t.B != e.A && t.B != e.B ? t.B : t.C;
    private static (int A, int B) Key(int a, int b) => a < b ? (a, b) : (b, a);
    private static IEnumerable<(int A, int B)> Edges((int A, int B, int C) t) =>
        new[] { Key(t.A, t.B), Key(t.B, t.C), Key(t.C, t.A) };
    private static Dictionary<(int A, int B), HashSet<int>> Adjacency(IReadOnlyList<(int A, int B, int C)> triangles)
    {
        var result = new Dictionary<(int A, int B), HashSet<int>>();
        for (int i = 0; i < triangles.Count; i++)
            foreach (var edge in Edges(triangles[i]))
            {
                if (!result.TryGetValue(edge, out var uses)) result[edge] = uses = new();
                uses.Add(i);
            }
        return result;
    }
}