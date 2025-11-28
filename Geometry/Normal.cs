namespace Geometry;

// Normal = unit vector for surfaces
// Separate type so Codex/GPT can’t confuse “any vector” with “unit normal”.
// Not a general Vector: represents only normalized directions.
public readonly struct Normal
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    private Normal(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Normal FromVector(Vector v)
    {
        var len = v.Length();
        if (len == 0)
            throw new ArgumentException("Cannot create normal from zero vector.", nameof(v));

        var inv = 1.0 / len;
        return new Normal(v.X * inv, v.Y * inv, v.Z * inv);
    }

    // Normals remain a distinct type; provide specific operations to avoid mixing with vectors.
    public double Dot(Vector v) => X * v.X + Y * v.Y + Z * v.Z;

    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
}

// Rule of thumb:
// “Normals are always Normal, never Vector. To get a normal, use Normal.FromVector.”
// That alone stops Codex from casually stuffing random non-unit vectors into a “normal”.
