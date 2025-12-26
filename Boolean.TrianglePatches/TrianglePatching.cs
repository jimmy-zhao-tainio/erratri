using Boolean.Intersection.Indexing;
using Boolean.Intersection.Topology;

namespace Boolean;

public static class TrianglePatching
{
    public static TrianglePatches Run(
        IntersectionGraph graph,
        IntersectionIndex index,
        MeshA topologyA,
        MeshB topologyB)
    {
        return TrianglePatchingCore.Run(graph, index, topologyA, topologyB);
    }
}
