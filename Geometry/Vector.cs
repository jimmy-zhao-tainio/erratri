namespace Geometry;

public readonly struct Vector
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public Vector(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector operator +(Vector a, Vector b)
        => new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector operator -(Vector a, Vector b)
        => new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector operator *(Vector v, double s)
        => new Vector(v.X * s, v.Y * s, v.Z * s);

    public static Vector operator *(double s, Vector v) => v * s;

    public double Dot(Vector b)
        => X * b.X + Y * b.Y + Z * b.Z;

    public Vector Cross(Vector b)
        => new Vector(
            Y * b.Z - Z * b.Y,
            Z * b.X - X * b.Z,
            X * b.Y - Y * b.X);

    public double Length() => Math.Sqrt(Dot(this));

    public Vector Normalized()
    {
        var len = Length();
        return len == 0 ? this : this * (1.0 / len);
    }
}
