using System;
using System.Linq;
using System.Numerics;
using Geometry;

namespace ConstrainedTriangulator;

// Filtered predicates with exact dyadic arithmetic on the input doubles.
// The exact fallback also makes cocircular ties deterministic (no flip).
internal static class DelaunayPredicates
{
    internal static int Orient(RealPoint2D a, RealPoint2D b, RealPoint2D c)
    {
        double l = (b.X - a.X) * (c.Y - a.Y), r = (b.Y - a.Y) * (c.X - a.X);
        double det = l - r;
        if (double.IsFinite(det) && Math.Abs(det) > 1e-14 * (Math.Abs(l) + Math.Abs(r))) return Math.Sign(det);
        var v = Integers(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        return ((v[2] - v[0]) * (v[5] - v[1]) - (v[3] - v[1]) * (v[4] - v[0])).Sign;
    }

    internal static int InCircle(RealPoint2D a, RealPoint2D b, RealPoint2D c, RealPoint2D d)
    {
        double ax = a.X - d.X, ay = a.Y - d.Y, bx = b.X - d.X, by = b.Y - d.Y, cx = c.X - d.X, cy = c.Y - d.Y;
        double al = ax * ax + ay * ay, bl = bx * bx + by * by, cl = cx * cx + cy * cy;
        double det = al * (bx * cy - by * cx) - bl * (ax * cy - ay * cx) + cl * (ax * by - ay * bx);
        double permanent = al * (Math.Abs(bx * cy) + Math.Abs(by * cx)) + bl * (Math.Abs(ax * cy) + Math.Abs(ay * cx)) + cl * (Math.Abs(ax * by) + Math.Abs(ay * bx));
        if (double.IsFinite(det) && Math.Abs(det) > 1e-13 * permanent) return Math.Sign(det);
        var v = Integers(a.X, a.Y, b.X, b.Y, c.X, c.Y, d.X, d.Y);
        var x = v[0] - v[6]; var y = v[1] - v[7]; var u = v[2] - v[6]; var w = v[3] - v[7]; var s = v[4] - v[6]; var t = v[5] - v[7];
        return ((x * x + y * y) * (u * t - w * s) - (u * u + w * w) * (x * t - y * s) + (s * s + t * t) * (x * w - y * u)).Sign;
    }

    private static BigInteger[] Integers(params double[] values)
    {
        var parts = new (BigInteger Mantissa, int Exponent)[values.Length];
        int min = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (!double.IsFinite(values[i])) throw new ArgumentException("Delaunay coordinates must be finite.");
            long bits = BitConverter.DoubleToInt64Bits(values[i]);
            int exponent = (int)((bits >> 52) & 2047);
            long fraction = bits & 0x000fffffffffffffL;
            BigInteger mantissa = exponent == 0 ? fraction : fraction | (1L << 52);
            if (bits < 0) mantissa = -mantissa;
            int power = exponent == 0 ? -1074 : exponent - 1075;
            parts[i] = (mantissa, power);
            if (!mantissa.IsZero) min = Math.Min(min, power);
        }
        return parts.Select(p => p.Mantissa.IsZero ? BigInteger.Zero : p.Mantissa << (p.Exponent - min)).ToArray();
    }
}