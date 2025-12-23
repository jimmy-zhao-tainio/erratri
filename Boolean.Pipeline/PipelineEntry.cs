using System;
using Geometry;
using Boolean;
using Boolean.Intersection.Graph.Index;
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

        var set = new IntersectionSet(meshA.Triangles, meshB.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);

        var classification = PatchClassifier.Classify(set, patches);
        var selected = BooleanPatchClassifier.Select(op, classification);

        var assemblyOutput = AssemblyEntry.Run(new AssemblyInput(graph, patches, selected));
        return assemblyOutput.Mesh;
    }
}
