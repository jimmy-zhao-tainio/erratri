namespace Geometry.Predicates;

public static class TetrahedronPredicates
{
    public static bool IsPointInsideStrict(in Tetrahedron tetrahedron, in Point point)
    {
        static bool IsOnNegativeSide(in Triangle triangle, in Point q)
            => Plane.FromTriangle(triangle).Side(q) < 0;

        return IsOnNegativeSide(tetrahedron.ABC, point)
            && IsOnNegativeSide(tetrahedron.ABD, point)
            && IsOnNegativeSide(tetrahedron.ACD, point)
            && IsOnNegativeSide(tetrahedron.BCD, point);
    }

    private static bool TriangleMatchesAny(in Triangle triangle, in Tetrahedron t)
        => TrianglePredicates.IsSame(triangle, t.ABC) ||
           TrianglePredicates.IsSame(triangle, t.ABD) ||
           TrianglePredicates.IsSame(triangle, t.ACD) ||
           TrianglePredicates.IsSame(triangle, t.BCD);

    public static bool SharesTriangle(in Tetrahedron first, in Tetrahedron second)
        => TriangleMatchesAny(first.ABC, second) ||
           TriangleMatchesAny(first.ABD, second) ||
           TriangleMatchesAny(first.ACD, second) ||
           TriangleMatchesAny(first.BCD, second);

    public static bool IsSolid(IReadOnlyList<Tetrahedron> tetrahedra)
    {
        if (tetrahedra is null) throw new ArgumentNullException(nameof(tetrahedra));
        if (tetrahedra.Count == 0) return false;

        for (int i = 0; i < tetrahedra.Count; i++)
        {
            var tetrahedron = tetrahedra[i];
            if (HasSharedTriangle(tetrahedron, tetrahedra, i)) continue;
            return false;
        }
        return true;
    }

    private static bool HasSharedTriangle(in Tetrahedron tetrahedron, IReadOnlyList<Tetrahedron> tetrahedra, int selfIndex)
    {
        for (int j = 0; j < tetrahedra.Count; j++)
        {
            if (j == selfIndex) continue;
            if (SharesTriangle(tetrahedron, tetrahedra[j])) return true;
        }
        return false;
    }
}
