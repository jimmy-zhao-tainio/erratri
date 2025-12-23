namespace Boolean.Intersection;

public static class GraphEntry
{
    public static global::Boolean.IntersectionGraph Run(global::Boolean.IntersectionSet set)
    {
        return global::Boolean.IntersectionGraph.FromIntersectionSet(set);
    }
}
