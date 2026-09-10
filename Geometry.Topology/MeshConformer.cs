using Geometry;

namespace Geometry.Topology;

/// <summary>Propagates existing seam vertices to every incident triangle edge.</summary>
public static class MeshConformer
{
    public static int SplitEdges(IReadOnlyList<RealPoint> vertices, List<(int A, int B, int C)> triangles,
        List<string>? provenance = null)
    {
        var sorted = new int[3][];
        double Coordinate(int i, int axis) => axis == 0 ? vertices[i].X : axis == 1 ? vertices[i].Y : vertices[i].Z;
        for (int axis = 0; axis < 3; axis++)
        {
            int dim = axis;
            sorted[axis] = Enumerable.Range(0, vertices.Count).OrderBy(i => Coordinate(i, dim)).ToArray();
        }
        var chains = new Dictionary<(int, int), List<int>>();
        List<int> Chain(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!chains.TryGetValue(key, out var chain))
            {
                a = key.Item1; b = key.Item2;
                var pa = vertices[a]; var pb = vertices[b];
                var ab = RealVector.FromPoints(pa, pb);
                double length2 = ab.Dot(ab);
                int axis = Math.Abs(ab.X) >= Math.Abs(ab.Y) ? 0 : 1;
                if (Math.Abs(ab.Z) > Math.Abs(axis == 0 ? ab.X : ab.Y)) axis = 2;
                double low = Math.Min(Coordinate(a, axis), Coordinate(b, axis));
                double high = Math.Max(Coordinate(a, axis), Coordinate(b, axis));
                var ordered = sorted[axis];
                int start = 0, end = ordered.Length;
                while (start < end)
                {
                    int mid = (start + end) / 2;
                    if (Coordinate(ordered[mid], axis) <= low) start = mid + 1; else end = mid;
                }
                var interior = new List<(double U, int Id)>();
                for (int i = start; i < ordered.Length && Coordinate(ordered[i], axis) < high; i++)
                {
                    int p = ordered[i];
                    if (p == a || p == b) continue;
                    var point = vertices[p];
                    double u = RealVector.FromPoints(pa, point).Dot(ab) / length2;
                    if (u <= 0 || u >= 1) continue;
                    var closest = new RealPoint(pa.X + u * ab.X, pa.Y + u * ab.Y, pa.Z + u * ab.Z);
                    if (point.DistanceSquared(closest) > Tolerances.MergeEpsilonSquared ||
                        point.DistanceSquared(pa) <= Tolerances.MergeEpsilonSquared ||
                        point.DistanceSquared(pb) <= Tolerances.MergeEpsilonSquared) continue;
                    interior.Add((u, p));
                }
                chain = interior.OrderBy(p => p.U).Select(p => p.Id).ToList();
                chain.Insert(0, a); chain.Add(b);
                chains.Add(key, chain);
            }
            return chain;
        }
        var output = new List<(int A, int B, int C)>();
        var source = provenance == null ? null : new List<string>();
        int splits = 0;
        for (int ti = 0; ti < triangles.Count; ti++)
        {
            var t = triangles[ti];
            var ring = new List<int>();
            foreach (var (a, b) in new[] { (t.A, t.B), (t.B, t.C), (t.C, t.A) })
            {
                var chain = Chain(a, b);
                if (chain[0] == a) ring.AddRange(chain.Take(chain.Count - 1));
                else for (int i = chain.Count - 1; i > 0; i--) ring.Add(chain[i]);
            }
            splits += ring.Count - 3;
            var normal = RealVector.FromPoints(vertices[t.A], vertices[t.B])
                .Cross(RealVector.FromPoints(vertices[t.A], vertices[t.C])).Normalized();
            double Side(int a, int b, int c) => RealVector.FromPoints(vertices[a], vertices[b])
                .Cross(RealVector.FromPoints(vertices[a], vertices[c])).Dot(normal);
            void Emit(int a, int b, int c) { output.Add((a, b, c)); if (source != null) source.Add(provenance![ti]); }
            while (ring.Count > 3)
            {
                int ear = -1; double best = 0;
                for (int i = 0; i < ring.Count; i++)
                {
                    int a = ring[(i + ring.Count - 1) % ring.Count], b = ring[i], c = ring[(i + 1) % ring.Count];
                    double area = Side(a, b, c);
                    if (area <= best) continue;
                    bool contains = ring.Any(p => p != a && p != b && p != c && Side(a, b, p) >= -Tolerances.MergeEpsilon &&
                        Side(b, c, p) >= -Tolerances.MergeEpsilon && Side(c, a, p) >= -Tolerances.MergeEpsilon);
                    if (contains) continue;
                    ear = i; best = area;
                }
                if (ear < 0) throw new InvalidOperationException("Cannot conform a triangle boundary without degeneracy.");
                Emit(ring[(ear + ring.Count - 1) % ring.Count], ring[ear], ring[(ear + 1) % ring.Count]);
                ring.RemoveAt(ear);
            }
            Emit(ring[0], ring[1], ring[2]);
        }
        triangles.Clear(); triangles.AddRange(output);
        if (provenance != null) { provenance.Clear(); provenance.AddRange(source!); }
        return splits;
    }
}