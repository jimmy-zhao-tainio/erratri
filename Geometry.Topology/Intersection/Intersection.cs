using Geometry;
using Geometry.Predicates;

namespace Geometry.Topology;


public static class IntersectionTypes
{
    public static IntersectionType Classify(in Triangle a, in Triangle b)
    {
        // First: coplanar vs non-coplanar.
        if (TrianglePredicates.IsCoplanar(in a, in b))
        {
            // Coplanar: area â†’ segment â†’ point â†’ none.
            if (TrianglePredicates.HasAreaIntersectionCoplanar(in a, in b))
                return IntersectionType.Area;
            if (TrianglePredicates.HasSegmentIntersectionCoplanar(in a, in b))
                return IntersectionType.Segment;
            if (TrianglePredicates.HasPointIntersectionCoplanar(in a, in b))
                return IntersectionType.Point;
            return IntersectionType.None;
        }
        else
        {
            // Non-coplanar: only segment or point (no area possible).
            if (TrianglePredicates.HasSegmentIntersectionNonCoplanar(in a, in b))
                return IntersectionType.Segment;
            if (TrianglePredicates.HasPointIntersectionNonCoplanar(in a, in b))
                return IntersectionType.Point;
            return IntersectionType.None;
        }
    }

    public static bool Any(in Triangle a, in Triangle b)
    {
        return Classify(in a, in b) != IntersectionType.None;
    }
}

