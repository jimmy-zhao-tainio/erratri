using System;

namespace Geometry;

public readonly struct RealVector
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public RealVector(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static RealVector FromPoints(in RealPoint from, in RealPoint to)
        => new RealVector(to.X - from.X, to.Y - from.Y, to.Z - from.Z);

    public double Dot(in RealVector other)
        => X * other.X + Y * other.Y + Z * other.Z;

    public RealVector Cross(in RealVector other)
        => new RealVector(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X);

    public double Length() => Math.Sqrt(Dot(this));

    public RealVector Normalized()
    {
        double len = Length();
        if (len <= 0) return this;
        double inv = 1.0 / len;
        return new RealVector(X * inv, Y * inv, Z * inv);
    }

    public static RealVector operator +(RealVector a, RealVector b)
        => new RealVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static RealVector operator -(RealVector a, RealVector b)
        => new RealVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static RealVector operator *(RealVector v, double s)
        => new RealVector(v.X * s, v.Y * s, v.Z * s);

    public static RealVector operator *(double s, RealVector v) => v * s;
}
