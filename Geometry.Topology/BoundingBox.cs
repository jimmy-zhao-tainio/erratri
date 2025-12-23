using Geometry;
using System.Collections.Generic;

namespace Geometry.Topology;

public readonly struct BoundingBox
{
    public readonly Point Min;
    public readonly Point Max;

    public BoundingBox(Point min, Point max)
    {
        Min = min;
        Max = max;
    }

    public double MaximumRayLength
    {
        get
        {
            double dx = Max.X - Min.X;
            double dy = Max.Y - Min.Y;
            double dz = Max.Z - Min.Z;
            double diagonal = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (diagonal <= 0) diagonal = 1.0;
            return diagonal * 2.0 + 1.0;
        }
    }

    public static BoundingBox FromPoints(in Point a, in Point b, in Point c)
    {
        long minX = Math.Min(a.X, Math.Min(b.X, c.X));
        long minY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        long minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z));
        long maxX = Math.Max(a.X, Math.Max(b.X, c.X));
        long maxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
        long maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
        return new BoundingBox(new Point(minX, minY, minZ), new Point(maxX, maxY, maxZ));
    }

    public static BoundingBox FromPoints(in RealPoint a, in RealPoint b)
    {
        double minX = Math.Min(a.X, b.X);
        double minY = Math.Min(a.Y, b.Y);
        double minZ = Math.Min(a.Z, b.Z);
        double maxX = Math.Max(a.X, b.X);
        double maxY = Math.Max(a.Y, b.Y);
        double maxZ = Math.Max(a.Z, b.Z);

        long lx0 = (long)Math.Floor(minX) - 1;
        long ly0 = (long)Math.Floor(minY) - 1;
        long lz0 = (long)Math.Floor(minZ) - 1;
        long lx1 = (long)Math.Ceiling(maxX) + 1;
        long ly1 = (long)Math.Ceiling(maxY) + 1;
        long lz1 = (long)Math.Ceiling(maxZ) + 1;

        return new BoundingBox(new Point(lx0, ly0, lz0), new Point(lx1, ly1, lz1));
    }

    public static BoundingBox FromTriangles(IReadOnlyList<Triangle> triangles)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        if (triangles.Count == 0) return Empty;

        var box = FromTriangle(triangles[0]);
        for (int i = 1; i < triangles.Count; i++)
        {
            var b = FromTriangle(triangles[i]);
            box = Union(in box, in b);
        }

        return box;
    }

    public static BoundingBox FromTriangle(in Triangle t)
        => FromPoints(t.P0, t.P1, t.P2);

    public static BoundingBox Union(in BoundingBox a, in BoundingBox b)
        => new BoundingBox(
            new Point(Math.Min(a.Min.X, b.Min.X), Math.Min(a.Min.Y, b.Min.Y), Math.Min(a.Min.Z, b.Min.Z)),
            new Point(Math.Max(a.Max.X, b.Max.X), Math.Max(a.Max.Y, b.Max.Y), Math.Max(a.Max.Z, b.Max.Z)));

    public bool IsEmpty
        => Min.X > Max.X
        || Min.Y > Max.Y
        || Min.Z > Max.Z;

    public bool Intersects(in BoundingBox other)
    {
        return !(other.Min.X > Max.X || other.Max.X < Min.X ||
                 other.Min.Y > Max.Y || other.Max.Y < Min.Y ||
                 other.Min.Z > Max.Z || other.Max.Z < Min.Z);
    }

    public static BoundingBox Empty => new BoundingBox(
        new Point(long.MaxValue, long.MaxValue, long.MaxValue),
        new Point(long.MinValue, long.MinValue, long.MinValue));
}

