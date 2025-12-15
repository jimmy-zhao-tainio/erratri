using System.Collections.Generic;
using Geometry;
using Topology;
using Xunit;
using WTetrahedron = World.Tetrahedron;

namespace Kernel.Tests;

public class BooleanOverlapCoplanarOwnershipTests
{
    [Fact]
    public void TetraOverlapIntersection_IsManifold_NoTripleEdges()
    {
        var a = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        // Overlap: base face shifted up in Z by 1, apex below it.
        var b = new WTetrahedron(
            new Point(0, 0, 1),
            new Point(2, 0, 1),
            new Point(0, 2, 1),
            new Point(0, 0, -1));

        var mesh = BooleanOps.Intersection(a.Mesh, b.Mesh);
        Assert.NotEmpty(mesh.Triangles);

        var edgeUse = new Dictionary<(int Min, int Max), int>();
        void AddEdge(int u, int v)
        {
            if (u == v) return;
            var key = u < v ? (u, v) : (v, u);
            edgeUse[key] = edgeUse.TryGetValue(key, out int n) ? n + 1 : 1;
        }

        for (int i = 0; i < mesh.Triangles.Count; i++)
        {
            var (aIdx, bIdx, cIdx) = mesh.Triangles[i];
            Assert.NotEqual(aIdx, bIdx);
            Assert.NotEqual(bIdx, cIdx);
            Assert.NotEqual(cIdx, aIdx);
            AddEdge(aIdx, bIdx);
            AddEdge(bIdx, cIdx);
            AddEdge(cIdx, aIdx);
        }

        foreach (var kvp in edgeUse)
        {
            Assert.True(kvp.Value == 2, $"Edge {kvp.Key} used {kvp.Value} times.");
        }
    }
}

