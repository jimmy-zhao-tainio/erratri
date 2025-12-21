using Geometry.Predicates;

namespace Tests.Geometry.Predicates;

internal static class PredicatesTestHelpers
{
    public static IEnumerable<Triangle> Faces(Tetrahedron t)
    {
        yield return t.ABC;
        yield return t.ABD;
        yield return t.ACD;
        yield return t.BCD;
    }

    public static Triangle FaceByVertices(
        in Tetrahedron t,
        in Point p0,
        in Point p1,
        in Point p2)
    {
        if (HasVertices(t.ABC, p0, p1, p2)) return t.ABC;
        if (HasVertices(t.ABD, p0, p1, p2)) return t.ABD;
        if (HasVertices(t.ACD, p0, p1, p2)) return t.ACD;
        if (HasVertices(t.BCD, p0, p1, p2)) return t.BCD;
        throw new InvalidOperationException("Requested face not found on tetrahedron.");
    }

    public static bool TryFindSharedFace(
        in Tetrahedron a,
        in Tetrahedron b,
        out Triangle faceA,
        out Triangle faceB)
    {
        foreach (var fa in Faces(a))
        {
            foreach (var fb in Faces(b))
            {
                if (TrianglePredicates.IsSame(fa, fb))
                {
                    faceA = fa;
                    faceB = fb;
                    return true;
                }
            }
        }
        faceA = default;
        faceB = default;
        return false;
    }

    public static bool HasVertices(
        in Triangle tri,
        in Point x,
        in Point y,
        in Point z)
    {
        int found = 0;
        if (tri.P0.Equals(x) || tri.P1.Equals(x) || tri.P2.Equals(x)) found++;
        if (tri.P0.Equals(y) || tri.P1.Equals(y) || tri.P2.Equals(y)) found++;
        if (tri.P0.Equals(z) || tri.P1.Equals(z) || tri.P2.Equals(z)) found++;
        return found == 3;
    }
}

