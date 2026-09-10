using System;
using System.Numerics;

namespace Geometry;

/// <summary>Exact incidence and physical-circle predicates on a plane in Z³.</summary>
public sealed class IntegerPlane
{
    private readonly Point origin;
    private readonly V normal;
    private readonly int first, second;

    public IntegerPlane(Point a, Point b, Point c)
    {
        origin = a;
        var n = Delta(b, a).Cross(Delta(c, a));
        if (n.Dot(n).IsZero) throw new ArgumentException("A plane requires three non-collinear points.");
        // Choose a nonsingular coordinate projection for ordering only. Circle
        // predicates below use the full 3D metric, never the projected metric.
        (first, second, normal) = !n.Z.IsZero ? (0, 1, n.Z.Sign > 0 ? n : -n)
            : !n.Y.IsZero ? (0, 2, n.Y.Sign < 0 ? n : -n)
            : (1, 2, n.X.Sign > 0 ? n : -n);
    }

    public bool Contains(Point p) => normal.Dot(Delta(p, origin)).IsZero;
    public int Orient(Point a, Point b, Point c) => normal.Dot(Delta(b, a).Cross(Delta(c, a))).Sign;
    public int Compare(Point a, Point b)
    {
        int order = Coordinate(a, first).CompareTo(Coordinate(b, first));
        return order != 0 ? order : Coordinate(a, second).CompareTo(Coordinate(b, second));
    }

    /// <summary>Positive inside, zero on, negative outside the circle of a,b,c.
    /// All four points must be in this plane; a,b,c must not be collinear.</summary>
    public int InCircle(Point a, Point b, Point c, Point d)
    {
        var x = Delta(a, d); var y = Delta(b, d); var z = Delta(c, d);
        var determinant = x.Dot(x) * normal.Dot(y.Cross(z))
            - y.Dot(y) * normal.Dot(x.Cross(z)) + z.Dot(z) * normal.Dot(x.Cross(y));
        return determinant.Sign * Orient(a, b, c);
    }

    public static bool OnSegment(Point a, Point b, Point p)
    {
        if (p.X < Math.Min(a.X, b.X) || p.X > Math.Max(a.X, b.X) ||
            p.Y < Math.Min(a.Y, b.Y) || p.Y > Math.Max(a.Y, b.Y) ||
            p.Z < Math.Min(a.Z, b.Z) || p.Z > Math.Max(a.Z, b.Z)) return false;
        var ab = Delta(b, a); var ap = Delta(p, a);
        var cross = ab.Cross(ap);
        return cross.Dot(cross).IsZero && ap.Dot(ab) >= 0 && ap.Dot(ab) <= ab.Dot(ab);
    }

    public static bool AreCollinear(Point a, Point b, Point c)
    {
        var cross = Delta(b, a).Cross(Delta(c, a));
        return cross.Dot(cross).IsZero;
    }

    /// <summary>Choose an interior lattice point near the midpoint, if one exists.</summary>
    public static bool TrySplit(Point a, Point b, out Point point)
    {
        var d = Delta(b, a);
        var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(d.X),
            BigInteger.GreatestCommonDivisor(BigInteger.Abs(d.Y), BigInteger.Abs(d.Z)));
        if (gcd <= 1) { point = default; return false; }
        var k = gcd / 2;
        point = new Point((long)(a.X + d.X / gcd * k), (long)(a.Y + d.Y / gcd * k), (long)(a.Z + d.Z / gcd * k));
        return true;
    }

    private static long Coordinate(Point p, int axis) => axis == 0 ? p.X : axis == 1 ? p.Y : p.Z;
    private static V Delta(Point a, Point b) => new((BigInteger)a.X - b.X, (BigInteger)a.Y - b.Y, (BigInteger)a.Z - b.Z);
    private readonly record struct V(BigInteger X, BigInteger Y, BigInteger Z)
    {
        public V Cross(V b) => new(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
        public BigInteger Dot(V b) => X * b.X + Y * b.Y + Z * b.Z;
        public static V operator -(V a) => new(-a.X, -a.Y, -a.Z);
    }
}