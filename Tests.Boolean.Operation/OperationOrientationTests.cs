using System;
using System.Collections.Generic;
using System.Linq;
using Boolean;
using Geometry;
using Geometry.Topology;
using World;
using Xunit;

namespace Tests.Boolean.Operation;

public class OperationOrientationTests
{
    [Fact]
    public void BoxTranslationPreservesOutwardWinding()
    {
        var box = new Box(400, 400, 400).Position(-200, -200, -200);
        Assert.Equal(64_000_000, Volume(BooleanMeshConverter.FromMesh(box.Mesh)), 6);
        AssertOriented(BooleanMeshConverter.FromMesh(box.Mesh));
    }

    [Fact]
    public void TwoTunnelsHaveExpectedVolumeAndOppositeEdgeDirections()
    {
        var a = new Box(400, 400, 400).Position(-200, -200, -200);
        var b = new Box(600, 200, 200).Position(-300, -100, -100);
        var c = new Box(200, 600, 200).Position(-100, -300, -100);
        var first = global::Boolean.Operation.DifferenceAB(a.Mesh, b.Mesh);
        Assert.Equal(48_000_000, Volume(first), 5);
        AssertOriented(first);
        var second = global::Boolean.Operation.DifferenceAB(BooleanMeshConverter.ToMesh(first), c.Mesh);
        Assert.Equal(40_000_000, Volume(second), 5);
        AssertOriented(second);
        AssertOriented(BooleanMeshConverter.FromMesh(BooleanMeshConverter.ToMesh(second)));
    }

    [Fact]
    public void ContainedDifferenceReversesCavityFaces()
    {
        var outer = new Box(20, 20, 20);
        var inner = new Box(10, 10, 10).Position(5, 5, 5);
        var result = global::Boolean.Operation.DifferenceAB(outer.Mesh, inner.Mesh);
        Assert.Equal(7000, Volume(result), 7);
        AssertOriented(result);
    }

    [Fact]
    public void FullCheeseRemainsClosedAfterEveryGridConversion()
    {
        Shape shape = new Box(400, 400, 400).Position(-200, -200, -200);
        var cutters = new List<Shape>{new Box(600,200,200).Position(-300,-100,-100),
            new Box(200,600,200).Position(-100,-300,-100),new Box(200,200,600).Position(-100,-100,-300)};
        foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                    cutters.Add(new Sphere(200, subdivisions: 3, center: new Point(x * 220, y * 220, z * 220)));
        double volume = Volume(BooleanMeshConverter.FromMesh(shape.Mesh));
        for (int i = 0; i < cutters.Count; i++)
        {
            var result = global::Boolean.Operation.DifferenceAB(shape.Mesh, cutters[i].Mesh);
            AssertOriented(result);
            var grid = BooleanMeshConverter.ToMesh(result);
            var snapped = BooleanMeshConverter.FromMesh(grid);
            try { AssertOriented(snapped); } catch (Exception e) { throw new Exception($"Grid conversion after cutter {i} failed", e); }
            double next = Volume(snapped);
            Assert.True(next > 0 && next < volume, $"Cutter {i}: {volume} -> {next}");
            volume = next; shape = new TestShape(grid);
        }
    }
    private sealed class TestShape : Shape { public TestShape(Mesh mesh) { Mesh = mesh; } }

    internal static double Volume(RealMesh m) => m.Triangles.Sum(t =>
    {
        var a = m.Vertices[t.A]; var b = m.Vertices[t.B]; var c = m.Vertices[t.C];
        return (a.X * (b.Y * c.Z - b.Z * c.Y) + a.Y * (b.Z * c.X - b.X * c.Z) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6;
    });
    internal static void AssertOriented(RealMesh m)
    {
        var uses = new Dictionary<(int, int), (int Count, int Direction)>();
        foreach (var t in m.Triangles)
            foreach (var e in new[] { (t.A, t.B), (t.B, t.C), (t.C, t.A) })
            {
                var key = e.Item1 < e.Item2 ? e : (e.Item2, e.Item1);
                uses.TryGetValue(key, out var prior);
                uses[key] = (prior.Count + 1, prior.Direction + (e.Item1 < e.Item2 ? 1 : -1));
            }
        Assert.NotEmpty(uses);
        Assert.All(uses, e => Assert.True(e.Value == (2, 0), $"Edge {e.Key}: {e.Value}"));
    }
}