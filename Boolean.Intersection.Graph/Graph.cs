using Boolean;

namespace Boolean.Intersection;

public static class Graph
{
    public static IntersectionGraph Run(IntersectionSet set)
    {
        return IntersectionGraph.FromIntersectionSet(set);
    }
}
