using System.Collections.Generic;
using Geometry;

namespace Boolean.Intersection;

public static class PairEntry
{
    public static global::Boolean.IntersectionSet Run(
        IReadOnlyList<Triangle> trianglesA,
        IReadOnlyList<Triangle> trianglesB)
    {
        return new global::Boolean.IntersectionSet(trianglesA, trianglesB);
    }
}
