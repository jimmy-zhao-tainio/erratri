using Geometry.Predicates.Internal;

namespace Geometry.Predicates;

public static class TrianglePredicates
{
    public static bool IsSame(
        in Triangle a,
        in Triangle b)
    {
        int found = 0;
        var a0 = a.P0; var a1 = a.P1; var a2 = a.P2;

        var b0 = b.P0;
        if (a0.Equals(b0) || a1.Equals(b0) || a2.Equals(b0)) found++;

        var b1 = b.P1;
        if (a0.Equals(b1) || a1.Equals(b1) || a2.Equals(b1)) found++;

        var b2 = b.P2;
        if (a0.Equals(b2) || a1.Equals(b2) || a2.Equals(b2)) found++;

        return found == 3;
    }

    public static bool IsCoplanar(in Triangle a, in Triangle b)
    {
        var plane = Plane.FromTriangle(a);
        return plane.Side(b.P0) == 0
            && plane.Side(b.P1) == 0
            && plane.Side(b.P2) == 0;
    }

    public static TriangleIntersection ClassifyCoplanar(in Triangle a, in Triangle b)
        => TriangleCoplanarIntersection.Classify(in a, in b);

    public static TriangleIntersection ClassifyNonCoplanar(in Triangle a, in Triangle b)
        => TriangleNonCoplanarIntersection.Classify(in a, in b);

    public static bool HasAreaIntersectionCoplanar(in Triangle a, in Triangle b)
        => ClassifyCoplanar(in a, in b).Type == TriangleIntersectionType.Area;

    public static bool HasSegmentIntersectionCoplanar(in Triangle a, in Triangle b)
    {
        var type = ClassifyCoplanar(in a, in b).Type;
        return type == TriangleIntersectionType.Segment
            || type == TriangleIntersectionType.Area;
    }

    public static bool HasPointIntersectionCoplanar(in Triangle a, in Triangle b)
    {
        var type = ClassifyCoplanar(in a, in b).Type;
        return type == TriangleIntersectionType.Point
            || type == TriangleIntersectionType.Segment
            || type == TriangleIntersectionType.Area;
    }

    public static bool HasSegmentIntersectionNonCoplanar(in Triangle a, in Triangle b)
        => ClassifyNonCoplanar(in a, in b).Type == TriangleIntersectionType.Segment;

    public static bool HasPointIntersectionNonCoplanar(in Triangle a, in Triangle b)
    {
        var type = ClassifyNonCoplanar(in a, in b).Type;
        return type == TriangleIntersectionType.Point
            || type == TriangleIntersectionType.Segment;
    }
}

