using System.Numerics;
using Geometry;
using Geometry.Topology;

namespace World;

/// <summary>Connects corresponding vertices of two or more convex polygon sections.
/// Optional inner rings create a hollow solid with rims at both ends. Side quads
/// are triangulated. Sections must be ordered along the loft without self-intersections;
/// global geometric self-intersection is not checked.</summary>
public sealed class Loft : Shape
{
    public Loft(IEnumerable<LoftSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        var rings = sections.ToArray();
        if (rings.Length < 2 || rings.Any(s => s == null))
            throw new ArgumentException("A loft requires at least two non-null sections.");
        int count = rings[0].Outer.Count;
        bool hollow = rings[0].Inner != null;
        if (rings.Any(s => s.Outer.Count != count || (s.Inner != null) != hollow))
            throw new ArgumentException("All sections must have matching vertex counts and hole presence.");
        var triangles = new List<Triangle>();
        void Add(Point a, Point b, Point c)
        {
            if (IntegerPlane.AreCollinear(a, b, c))
                throw new ArgumentException("Loft produced a degenerate triangle; check section spacing and correspondence.");
            triangles.Add(Triangle.FromWinding(a, b, c));
        }
        void Connect(IReadOnlyList<Point> a, IReadOnlyList<Point> b, bool reverse)
        {
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                if (reverse) { Add(a[i], b[j], a[j]); Add(a[i], b[i], b[j]); }
                else { Add(a[i], a[j], b[j]); Add(a[i], b[j], b[i]); }
            }
        }
        for (int k = 1; k < rings.Length; k++)
        {
            Connect(rings[k - 1].Outer, rings[k].Outer, false);
            if (hollow) Connect(rings[k - 1].Inner!, rings[k].Inner!, true);
        }
        void Cap(LoftSection section, bool reverse)
        {
            void Emit(Point a, Point b, Point c) { if (reverse) Add(a, c, b); else Add(a, b, c); }
            if (section.Inner is { } inner)
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count;
                    Emit(section.Outer[i], section.Outer[j], inner[j]);
                    Emit(section.Outer[i], inner[j], inner[i]);
                }
            else
                for (int i = 1; i + 1 < count; i++) Emit(section.Outer[0], section.Outer[i], section.Outer[i + 1]);
        }
        Cap(rings[0], true);
        Cap(rings[^1], false);

        // Exact signed volume sets outward winding without relying on an axis.
        BigInteger volume6 = 0;
        var edges = new Dictionary<(Point, Point), (int Count, int Direction)>();
        int Compare(Point a, Point b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.Z.CompareTo(b.Z);
        foreach (var t in triangles)
        {
            var a = t.P0; var b = t.P1; var c = t.P2;
            volume6 += (BigInteger)a.X * ((BigInteger)b.Y * c.Z - (BigInteger)b.Z * c.Y)
                + (BigInteger)a.Y * ((BigInteger)b.Z * c.X - (BigInteger)b.X * c.Z)
                + (BigInteger)a.Z * ((BigInteger)b.X * c.Y - (BigInteger)b.Y * c.X);
            foreach (var (u, v) in new[] { (a, b), (b, c), (c, a) })
            {
                bool forward = Compare(u, v) < 0;
                var key = forward ? (u, v) : (v, u);
                edges.TryGetValue(key, out var prior);
                edges[key] = (prior.Count + 1, prior.Direction + (forward ? 1 : -1));
            }
        }
        if (volume6.IsZero || edges.Any(e => e.Value != (2, 0)))
            throw new ArgumentException("Loft must enclose nonzero volume with closed, oppositely directed edges.");
        Mesh = new Mesh(volume6.Sign > 0 ? triangles : triangles.Select(t => Triangle.FromWinding(t.P0, t.P2, t.P1)));
    }
}
