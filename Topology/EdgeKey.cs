using Geometry;

namespace Topology;

// Order-agnostic, integer-only key for an undirected edge defined by two grid Points.
public readonly struct EdgeKey : IEquatable<EdgeKey>
{
    public readonly Point A;
    public readonly Point B;

    public EdgeKey(in Point p, in Point q)
    {
        if (Lt(p, q)) { A = p; B = q; }
        else { A = q; B = p; }
    }

    public static EdgeKey FromPoints(in Point p, in Point q) => new EdgeKey(p, q);

    public bool Equals(EdgeKey other) => A.Equals(other.A) && B.Equals(other.B);
    public override bool Equals(object? obj) => obj is EdgeKey e && Equals(e);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(A.X); hc.Add(A.Y); hc.Add(A.Z);
        hc.Add(B.X); hc.Add(B.Y); hc.Add(B.Z);
        return hc.ToHashCode();
    }

    public override string ToString() => $"[{A.X},{A.Y},{A.Z}]-[{B.X},{B.Y},{B.Z}]";

    private static bool Lt(in Point p, in Point q)
    {
        if (p.X != q.X) return p.X < q.X;
        if (p.Y != q.Y) return p.Y < q.Y;
        return p.Z < q.Z;
    }
}
