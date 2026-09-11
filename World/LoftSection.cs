using Geometry;

namespace World;

/// <summary>An exactly planar, strictly convex boundary with an optional convex hole.
/// Rings use the same winding and corresponding vertex indices. No closing vertex is repeated.</summary>
public sealed class LoftSection
{
    public IReadOnlyList<Point> Outer { get; }
    public IReadOnlyList<Point>? Inner { get; }

    public LoftSection(IEnumerable<Point> outer, IEnumerable<Point>? inner = null)
    {
        ArgumentNullException.ThrowIfNull(outer);
        var a = outer.ToArray();
        var b = inner?.ToArray();
        if (a.Length < 3) throw new ArgumentException("A section requires at least three vertices.");
        var plane = new IntegerPlane(a[0], a[1], a[2]);
        int winding = plane.Orient(a[0], a[1], a[2]);
        void CheckRing(Point[] ring)
        {
            if (ring.Length != a.Length || ring.Distinct().Count() != ring.Length || ring.Any(p => !plane.Contains(p)))
                throw new ArgumentException("Section rings must have matching counts, distinct vertices and exact coplanarity.");
            for (int i = 0; i < ring.Length; i++)
                for (int j = 0; j < ring.Length; j++)
                    if (j != i && j != (i + 1) % ring.Length &&
                        plane.Orient(ring[i], ring[(i + 1) % ring.Length], ring[j]) != winding)
                        throw new ArgumentException("Section rings must be strictly convex with the same winding.");
        }
        CheckRing(a);
        if (b != null)
        {
            CheckRing(b);
            for (int i = 0; i < a.Length; i++)
            {
                int j = (i + 1) % a.Length;
                if (b.Any(p => plane.Orient(a[i], a[j], p) != winding))
                    throw new ArgumentException("The inner ring must lie strictly inside the outer ring.");
                if (plane.Orient(a[i], a[j], b[j]) != winding ||
                    plane.Orient(a[i], b[j], b[i]) != winding)
                    throw new ArgumentException("Inner vertices must correspond to outer vertices without crossed cap strips.");
            }
        }
        Outer = Array.AsReadOnly(a);
        Inner = b == null ? null : Array.AsReadOnly(b);
    }
}
