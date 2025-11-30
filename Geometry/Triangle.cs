namespace Geometry;

// Standalone triangle with outward unit normal in grid space.
// Mirrors the former nested Tetrahedron.Triangle semantics.
public readonly struct Triangle
{
    public readonly Point P0;
    public readonly Point P1;
    public readonly Point P2;
    public readonly RealNormal Normal; // outward, unit length

    // Construct a triangle from three points and a "missing" point.
    // The missing point is NOT part of the triangle. It is a fourth vertex
    // (typically from a tetrahedron) used only to orient the triangle's normal
    // so that it points away from that missing point.
    public Triangle(Point p0, Point p1, Point p2, Point missing)
    {
        if (HasZeroArea(in p0, in p1, in p2))
            throw new System.Exception();

        P0 = p0;
        P1 = p1;
        P2 = p2;

        var e1 = Vector128.FromPoints(p0, p1);
        var e2 = Vector128.FromPoints(p0, p2);
        var nc = Vector128.Cross(e1, e2);
        var n = new RealVector((double)nc.X, (double)nc.Y, (double)nc.Z);
        var normal = RealNormal.FromVector(n);

        // Ensure outward: decide using exact Int128 dot to avoid floating ambiguity.
        var missDelta = Vector128.FromPoints(p0, missing);
        var sign = Vector128.Dot(nc, missDelta);
        if (sign >= 0)
        {
            normal = RealNormal.FromVector(n * -1.0);
        }

        Normal = normal;
    }

    // Construct from three points assuming given winding already encodes outward orientation.
    // Normal is computed directly from P0->P1 and P0->P2.
    public static Triangle FromWinding(Point p0, Point p1, Point p2)
    {
        if (HasZeroArea(in p0, in p1, in p2))
            throw new System.Exception();

        var e1 = new RealVector((double)p1.X - p0.X, (double)p1.Y - p0.Y, (double)p1.Z - p0.Z);
        var e2 = new RealVector((double)p2.X - p0.X, (double)p2.Y - p0.Y, (double)p2.Z - p0.Z);
        var n = e1.Cross(e2).Normalized();
        return new Triangle(p0, p1, p2, n);
    }

    // Private ctor to set exact normal when already computed as unit vector.
    private Triangle(Point p0, Point p1, Point p2, RealVector unitNormal)
    {
        P0 = p0; P1 = p1; P2 = p2; Normal = RealNormal.FromVector(unitNormal);
    }

    internal static bool HasZeroArea(in Point p0, in Point p1, in Point p2)
    {
        long v0x = p1.X - p0.X;
        long v0y = p1.Y - p0.Y;
        long v0z = p1.Z - p0.Z;

        long v1x = p2.X - p0.X;
        long v1y = p2.Y - p0.Y;
        long v1z = p2.Z - p0.Z;

        long cx = v0y * v1z - v0z * v1y;
        long cy = v0z * v1x - v0x * v1z;
        long cz = v0x * v1y - v0y * v1x;

        long lenSq = cx * cx + cy * cy + cz * cz;
        return lenSq == 0;
    }

}
