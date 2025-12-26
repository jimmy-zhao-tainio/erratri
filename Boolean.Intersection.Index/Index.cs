using Boolean;
using Boolean.Intersection.Indexing;

namespace Boolean.Intersection;

public static class Index
{
    public static TriangleIntersectionIndex Run(IntersectionGraph graph)
    {
        return TriangleIntersectionIndex.Run(graph);
    }
}
