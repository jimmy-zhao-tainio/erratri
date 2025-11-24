using System;

namespace Geometry;

public readonly struct RealNormal
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public RealNormal(in RealVector direction)
    {
        double length = direction.Length();
        if (length <= 0)
        {
            X = 1;
            Y = 0;
            Z = 0;
        }
        else
        {
            double inv = 1.0 / length;
            X = direction.X * inv;
            Y = direction.Y * inv;
            Z = direction.Z * inv;
        }
    }
}
