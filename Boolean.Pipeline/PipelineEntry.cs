using System;
using Geometry;

using Geometry.Topology;

namespace Boolean.Pipeline;

public static class PipelineEntry
{
    public static RealMesh Run(Mesh left, Mesh right, BooleanOperationType op)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));

        var meshA = left;
        var meshB = right;

        var set = Intersection.Pair.Run(meshA.Triangles, meshB.Triangles);
        var graph = Intersection.Graph.Run(set);
        var index = Intersection.Index.Run(graph);
        var topoA = Intersection.Topology.MeshA.Run(graph, index);
        var topoB = Intersection.Topology.MeshB.Run(graph, index);
        var patches = TrianglePatching.Run(graph, index, topoA, topoB);

        var classification = Classification.Run(set, patches);
        var selected = Selection.Run(op, classification);

        var assemblyOutput = Assembly.Run(new AssemblyInput(graph, patches, selected));
        return assemblyOutput.Mesh;
    }
}
