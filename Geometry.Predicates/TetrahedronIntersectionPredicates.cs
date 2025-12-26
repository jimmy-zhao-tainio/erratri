namespace Geometry.Predicates;

public static class TetrahedronIntersectionPredicates
{
    public static bool Intersects(in Tetrahedron a, in Tetrahedron b)
    {
        if (!BoundingBoxOverlap(a, b)) return false;

        if (TetrahedronPredicates.IsPointInsideStrict(b, a.A) || TetrahedronPredicates.IsPointInsideStrict(b, a.B) ||
            TetrahedronPredicates.IsPointInsideStrict(b, a.C) || TetrahedronPredicates.IsPointInsideStrict(b, a.D))
            return true;

        if (TetrahedronPredicates.IsPointInsideStrict(a, b.A) || TetrahedronPredicates.IsPointInsideStrict(a, b.B) ||
            TetrahedronPredicates.IsPointInsideStrict(a, b.C) || TetrahedronPredicates.IsPointInsideStrict(a, b.D))
            return true;

        return false;
    }

    private static bool BoundingBoxOverlap(in Tetrahedron a, in Tetrahedron b)
    {
        (long minX, long minY, long minZ, long maxX, long maxY, long maxZ) Box(in Tetrahedron t)
        {
            long minX = Math.Min(Math.Min(t.A.X, t.B.X), Math.Min(t.C.X, t.D.X));
            long minY = Math.Min(Math.Min(t.A.Y, t.B.Y), Math.Min(t.C.Y, t.D.Y));
            long minZ = Math.Min(Math.Min(t.A.Z, t.B.Z), Math.Min(t.C.Z, t.D.Z));
            long maxX = Math.Max(Math.Max(t.A.X, t.B.X), Math.Max(t.C.X, t.D.X));
            long maxY = Math.Max(Math.Max(t.A.Y, t.B.Y), Math.Max(t.C.Y, t.D.Y));
            long maxZ = Math.Max(Math.Max(t.A.Z, t.B.Z), Math.Max(t.C.Z, t.D.Z));
            return (minX, minY, minZ, maxX, maxY, maxZ);
        }

        var ab = Box(a); var bb = Box(b);
        if (ab.maxX <= bb.minX || bb.maxX <= ab.minX) return false;
        if (ab.maxY <= bb.minY || bb.maxY <= ab.minY) return false;
        if (ab.maxZ <= bb.minZ || bb.maxZ <= ab.minZ) return false;
        return true;
    }
}
