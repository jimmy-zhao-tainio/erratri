using System.Linq;
using Geometry;
using Boolean;
using World;
using Xunit;
using WTetrahedron = World.Tetrahedron;
using Boolean.Intersection.Graph.Index;

namespace Tests.Boolean.Selection;

public class SelectionTests
{
    private static PatchClassification BuildClassification(WTetrahedron a, WTetrahedron b)
    {
        var set = new IntersectionSet(a.Mesh.Triangles, b.Mesh.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);
        return PatchClassifier.Classify(set, patches);
    }

    [Fact]
    public void NestedTetra_ClassificationMatchesOperation()
    {
        var inner = new WTetrahedron(
            new Point(1, 1, 1),
            new Point(2, 1, 1),
            new Point(1, 2, 1),
            new Point(1, 1, 2));
        var outer = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(10, 0, 0),
            new Point(0, 10, 0),
            new Point(0, 0, 10));
        var classification = BuildClassification(inner, outer);
        var intersection = BooleanPatchClassifier.Select(BooleanOperationType.Intersection, classification);
        Assert.True(intersection.FromMeshA.Count > 0); // inner surface retained
        Assert.Empty(intersection.FromMeshB);          // outer outside A
        var union = BooleanPatchClassifier.Select(BooleanOperationType.Union, classification);
        Assert.Empty(union.FromMeshA);                // inner hidden inside outer
        Assert.True(union.FromMeshB.Count > 0);       // outer kept
        var diffAB = BooleanPatchClassifier.Select(BooleanOperationType.DifferenceAB, classification);
        Assert.Empty(diffAB.FromMeshA);               // inner entirely removed
        Assert.Empty(diffAB.FromMeshB);               // outer not part of A\B
        var diffBA = BooleanPatchClassifier.Select(BooleanOperationType.DifferenceBA, classification);
        Assert.True(diffBA.FromMeshA.Count > 0);      // inner kept to cap hole
        Assert.True(diffBA.FromMeshB.Count > 0);      // outer shell kept
        var xor = BooleanPatchClassifier.Select(BooleanOperationType.SymmetricDifference, classification);
        Assert.Empty(xor.FromMeshA);                  // inner not part of xor since enclosed
        Assert.True(xor.FromMeshB.Count > 0);         // outer shell remains
    }

    [Fact]
    public void DisjointTetra_OnlyOutsidePatchesKept()
    {
        var a = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));
        var b = new WTetrahedron(
            new Point(100, 100, 100),
            new Point(102, 100, 100),
            new Point(100, 102, 100),
            new Point(100, 100, 102));
        var classification = BuildClassification(a, b);
        var union = BooleanPatchClassifier.Select(BooleanOperationType.Union, classification);
        Assert.True(union.FromMeshA.Count > 0);
        Assert.True(union.FromMeshB.Count > 0);
        var intersection = BooleanPatchClassifier.Select(BooleanOperationType.Intersection, classification);
        Assert.Empty(intersection.FromMeshA);
        Assert.Empty(intersection.FromMeshB);
    }
}

