using System;

namespace Geometry.Predicates;

public enum TriangleIntersectionType
{
    None = 0,
    Point = 1,
    Segment = 2,
    Area = 3
}

public readonly struct TriangleIntersection
{
    public TriangleIntersectionType Type { get; }

    public TriangleIntersection(TriangleIntersectionType type)
    {
        Type = type;
    }
}
