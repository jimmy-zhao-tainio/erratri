using System.Collections.Generic;
using Geometry;

namespace Boolean.Intersection;

public static class Pair
{
    public static Boolean.IntersectionSet Run(
        IReadOnlyList<Triangle> trianglesA,
        IReadOnlyList<Triangle> trianglesB)
    {
        return new Boolean.IntersectionSet(trianglesA, trianglesB);
    }
}
