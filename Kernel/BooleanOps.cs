using System;
using System.Collections.Generic;
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

        bool debug = EdgeUseDiagnostics.DebugStage2Enabled;

        var set = new IntersectionSet(a.Triangles, b.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);

        if (debug)
        {
            var allA = new List<RealTriangle>();
            var allB = new List<RealTriangle>();
            for (int i = 0; i < patches.TrianglesA.Count; i++)
            {
                var g = patches.TrianglesA[i];
                for (int p = 0; p < g.Count; p++) allA.Add(g[p]);
            }
            for (int i = 0; i < patches.TrianglesB.Count; i++)
            {
                var g = patches.TrianglesB[i];
                for (int p = 0; p < g.Count; p++) allB.Add(g[p]);
            }

            var allPatchSet = new BooleanPatchSet(allA, allB);
            BooleanMeshAssembler.DebugPrintCheckpointAllPatches("ckpt_allPatches", allPatchSet);

            var allPatches = new List<RealTriangle>();
            for (int i = 0; i < patches.TrianglesA.Count; i++)
            {
                var g = patches.TrianglesA[i];
                for (int p = 0; p < g.Count; p++) allPatches.Add(g[p]);
            }
            for (int i = 0; i < patches.TrianglesB.Count; i++)
            {
                var g = patches.TrianglesB[i];
                for (int p = 0; p < g.Count; p++) allPatches.Add(g[p]);
            }

            EdgeUseDiagnostics.PrintEdgeUseFromRealTriangles("ckpt_patches_all", allPatches);
        }

        var classification = PatchClassifier.Classify(set, patches);
        var selected = BooleanPatchClassifier.Select(op, classification);

        if (debug)
        {
            var allSelected = new List<RealTriangle>(selected.FromMeshA.Count + selected.FromMeshB.Count);
            for (int i = 0; i < selected.FromMeshA.Count; i++) allSelected.Add(selected.FromMeshA[i]);
            for (int i = 0; i < selected.FromMeshB.Count; i++) allSelected.Add(selected.FromMeshB[i]);
            EdgeUseDiagnostics.PrintEdgeUseFromRealTriangles("ckpt_selected_all", allSelected);
        }

        return BooleanMeshAssembler.Assemble(selected);
    }
}
