using System;
using Geometry;
using Topology;
namespace Kernel;

// Simple facade around the existing boolean mesher for Mesh inputs.
public static class BooleanOps
{
    public static RealMesh Union(Mesh a, Mesh b) =>
        Run(BooleanOperation.Union, a, b);

    public static RealMesh Intersection(Mesh a, Mesh b) =>
        Run(BooleanOperation.Intersection, a, b);

    public static RealMesh DifferenceAB(Mesh a, Mesh b) =>
        Run(BooleanOperation.DifferenceAB, a, b);

    public static RealMesh DifferenceBA(Mesh a, Mesh b) =>
        Run(BooleanOperation.DifferenceBA, a, b);

    public static RealMesh SymmetricDifference(Mesh a, Mesh b) =>
        Run(BooleanOperation.SymmetricDifference, a, b);

    private static RealMesh Run(BooleanOperation op, Mesh a, Mesh b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var set = new IntersectionSet(a.Triangles, b.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);
        var classification = PatchClassifier.Classify(set, patches);
        var selected = BooleanPatchClassifier.Select(op, classification);
        return BooleanMeshAssembler.Assemble(selected);
    }
}
