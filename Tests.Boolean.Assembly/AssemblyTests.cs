using System.Linq;
using Geometry;
using Boolean;
using Geometry.Topology;
using World;
using Xunit;
using WTetrahedron = World.Tetrahedron;
using Boolean.Intersection.Graph.Index;

namespace Tests.Boolean.Assembly;

public class AssemblyTests
{
    private static RealMesh Build(BooleanOperationType op, WTetrahedron a, WTetrahedron b)
    {
        var set = new IntersectionSet(a.Mesh.Triangles, b.Mesh.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);
        var classification = PatchClassifier.Classify(set, patches);
        var selected = BooleanPatchClassifier.Select(op, classification);
        return BooleanMeshAssembler.Assemble(selected);
    }

    [Fact]
    public void DisjointUnion_KeepsBothShells()
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
        var mesh = Build(BooleanOperationType.Union, a, b);
        Assert.True(mesh.Triangles.Count >= 8);
    }

    [Fact]
    public void Intersection_RemovesOuterShell()
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
        var mesh = Build(BooleanOperationType.Intersection, inner, outer);
        // Expect only inner tetra patches to remain.
        Assert.True(mesh.Triangles.Count > 0);
        Assert.True(mesh.Vertices.Count < outer.Mesh.Triangles.Count * 3);
    }

    [Fact]
    public void NonManifoldEdge_DumpCase_MissingTwin()
    {
        // This is currently a placeholder: we need a deterministic reproducer for the
        // sphere-union non-manifold edge. For now, assert that a simple disjoint
        // union builds without throwing; real repro to be added once captured.
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
        var mesh = Build(BooleanOperationType.Union, a, b);
        Assert.NotEmpty(mesh.Triangles);
    }
}


