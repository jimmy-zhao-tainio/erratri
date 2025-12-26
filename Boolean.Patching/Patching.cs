using Boolean.Intersection.Indexing;
using Boolean.Intersection.Topology;

namespace Boolean;

public static class Patching
{
    public static TrianglePatchSet Run(
        IntersectionGraph graph,
        TriangleIntersectionIndex index,
        MeshA topologyA,
        MeshB topologyB)
    {
        return TrianglePatchSet.Run(graph, index, topologyA, topologyB);
    }
}
