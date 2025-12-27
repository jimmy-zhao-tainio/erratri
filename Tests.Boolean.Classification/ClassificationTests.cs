using System.Linq;
using Boolean;
using Boolean.Intersection.Indexing;
using Geometry;
using Geometry.Topology;
using World;
using Xunit;
using WTetrahedron = World.Tetrahedron;

using Boolean.Intersection.Topology;

namespace Tests.Boolean.Classification;

public class ClassificationTests
{
    [Fact]
    public void InnerTetraInsideOuter_ClassifiesInsideAndOutside()
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
        var result = BuildClassification(inner, outer);
        Assert.All(result.MeshA.SelectMany(p => p), pi => Assert.True(pi.IsInsideOtherMesh));
        Assert.All(result.MeshB.SelectMany(p => p), pi => Assert.False(pi.IsInsideOtherMesh));
    }

    [Fact]
    public void DisjointTetras_ClassifyOutsideForBoth()
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
        var result = BuildClassification(a, b);
        Assert.All(result.MeshA.SelectMany(p => p), pi => Assert.False(pi.IsInsideOtherMesh));
        Assert.All(result.MeshB.SelectMany(p => p), pi => Assert.False(pi.IsInsideOtherMesh));
    }

    private static PatchClassification BuildClassification(WTetrahedron a, WTetrahedron b)
    {
        var set = new IntersectionSet(a.Mesh.Triangles, b.Mesh.Triangles);
        var graph = global::Boolean.Intersection.Graph.Run(set);
        var index = global::Boolean.Intersection.Index.Run(graph);
        var topoA = MeshA.Run(graph, index);
        var topoB = MeshB.Run(graph, index);
        var patches = global::Boolean.TrianglePatching.Run(graph, index, topoA, topoB);
        return global::Boolean.Classification.Run(set, patches);
    }
}






