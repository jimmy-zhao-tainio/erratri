using Geometry;

namespace Geometry.Topology;

// Order-agnostic, integer-only key for a triangle defined by three grid Points.
public readonly struct TriangleKey : IEquatable<TriangleKey>
{
    public readonly Point A;
    public readonly Point B;
    public readonly Point C;

    public TriangleKey(in Point p0, in Point p1, in Point p2)
    {
        (A, B, C) = Sort3(p0, p1, p2);
    }

    public static TriangleKey FromPoints(in Point p0, in Point p1, in Point p2)
        => new TriangleKey(p0, p1, p2);

    public static TriangleKey FromTriangle(in Triangle t)
        => new TriangleKey(t.P0, t.P1, t.P2);

    public bool Equals(TriangleKey other)
        => A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C);

    public override bool Equals(object? obj)
        => obj is TriangleKey k && Equals(k);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(A.X); hc.Add(A.Y); hc.Add(A.Z);
        hc.Add(B.X); hc.Add(B.Y); hc.Add(B.Z);
        hc.Add(C.X); hc.Add(C.Y); hc.Add(C.Z);
        return hc.ToHashCode();
    }

    public override string ToString()
        => $"[{A.X},{A.Y},{A.Z}]-[{B.X},{B.Y},{B.Z}]-[{C.X},{C.Y},{C.Z}]";

    private static (Point, Point, Point) Sort3(in Point p0, in Point p1, in Point p2)
    {
        var a = p0; var b = p1; var c = p2;
        if (GreaterThan(a, b)) { var t = a; a = b; b = t; }
        if (GreaterThan(b, c)) { var t = b; b = c; c = t; }
        if (GreaterThan(a, b)) { var t = a; a = b; b = t; }
        return (a, b, c);
    }

    private static bool GreaterThan(in Point p, in Point q)
    {
        if (p.X != q.X) return p.X > q.X;
        if (p.Y != q.Y) return p.Y > q.Y;
        return p.Z > q.Z;
    }
}

