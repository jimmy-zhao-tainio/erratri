namespace Geometry;

public readonly struct QuantizedVertexKey : System.IEquatable<QuantizedVertexKey>
{
    public readonly long X;
    public readonly long Y;
    public readonly long Z;

    public QuantizedVertexKey(long x, long y, long z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static QuantizedVertexKey FromRealPoint(in RealPoint point)
    {
        double inv = 1.0 / Tolerances.TrianglePredicateEpsilon;
        long qx = (long)System.Math.Round(point.X * inv);
        long qy = (long)System.Math.Round(point.Y * inv);
        long qz = (long)System.Math.Round(point.Z * inv);
        return new QuantizedVertexKey(qx, qy, qz);
    }

    public bool Equals(QuantizedVertexKey other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is QuantizedVertexKey other && Equals(other);
    public override int GetHashCode() => System.HashCode.Combine(X, Y, Z);
}
