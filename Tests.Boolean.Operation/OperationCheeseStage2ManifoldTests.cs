using System.Collections.Generic;
using Geometry;
using Boolean;
using Geometry.Topology;
using World;
using Xunit;

namespace Tests.Boolean.Operation;

public class OperationCheeseStage2ManifoldTests
{
    [Fact]
    public void CubeMinusTunnelXMinusTunnelY_IsManifold()
    {
        // Matches the first two subtraction stages in Demo.Boolean.Mesh.Cheese.
        var cube = new Box(width: 400, depth: 400, height: 400).Position(-200, -200, -200);
        Shape tunnelX = new Box(width: 600, depth: 200, height: 200).Position(-300, -100, -100);
        Shape tunnelY = new Box(width: 200, depth: 600, height: 200).Position(-100, -300, -100);
        var stage1 = global::Boolean.Operation.DifferenceAB(cube.Mesh, tunnelX.Mesh);
        var stage1Mesh = BooleanMeshConverter.ToMesh(stage1);
        var stage2 = global::Boolean.Operation.DifferenceAB(stage1Mesh, tunnelY.Mesh);
        Assert.NotEmpty(stage2.Triangles);
        AssertManifoldByEdgeUse(stage2);
    }

    [Fact]
    public void CubeMinusTunnelXMinusTunnelY_RoofTouchOnly_IsManifold()
    {
        var cube = new Box(width: 400, depth: 400, height: 400).Position(-200, -200, -200);
        Shape tunnelX = new Box(width: 600, depth: 200, height: 200).Position(-300, -100, -100);
        Shape tunnelY = new Box(width: 200, depth: 600, height: 100).Position(-100, -300, 0);
        var stage1 = global::Boolean.Operation.DifferenceAB(cube.Mesh, tunnelX.Mesh);
        var stage1Mesh = BooleanMeshConverter.ToMesh(stage1);
        var stage2 = global::Boolean.Operation.DifferenceAB(stage1Mesh, tunnelY.Mesh);
        Assert.NotEmpty(stage2.Triangles);
        AssertManifoldByEdgeUse(stage2);
    }

    [Fact]
    public void CubeMinusTunnelXMinusTunnelY_NoCoplanarTouch_IsManifold()
    {
        var cube = new Box(width: 400, depth: 400, height: 400).Position(-200, -200, -200);
        Shape tunnelX = new Box(width: 600, depth: 200, height: 200).Position(-300, -100, -100);
        Shape tunnelY = new Box(width: 200, depth: 600, height: 98).Position(-100, -300, 1);
        var stage1 = global::Boolean.Operation.DifferenceAB(cube.Mesh, tunnelX.Mesh);
        var stage1Mesh = BooleanMeshConverter.ToMesh(stage1);
        var stage2 = global::Boolean.Operation.DifferenceAB(stage1Mesh, tunnelY.Mesh);
        Assert.NotEmpty(stage2.Triangles);
        AssertManifoldByEdgeUse(stage2);
    }

    private static void AssertManifoldByEdgeUse(RealMesh mesh)
    {
        var edgeUse = new Dictionary<(int Min, int Max), int>();
        void AddEdge(int u, int v)
        {
            if (u == v) return;
            var key = u < v ? (u, v) : (v, u);
            edgeUse[key] = edgeUse.TryGetValue(key, out int n) ? n + 1 : 1;
        }
        for (int i = 0; i < mesh.Triangles.Count; i++)
        {
            var (a, b, c) = mesh.Triangles[i];
            Assert.NotEqual(a, b);
            Assert.NotEqual(b, c);
            Assert.NotEqual(c, a);
            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }
        foreach (var kvp in edgeUse)
        {
            Assert.True(kvp.Value == 2, $"Edge {kvp.Key} used {kvp.Value} times.");
        }
    }
}


