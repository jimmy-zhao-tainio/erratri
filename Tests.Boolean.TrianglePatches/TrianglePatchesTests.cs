using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Boolean;
using Xunit;
using Boolean.Intersection.Indexing;

using Boolean.Intersection.Topology;

namespace Tests.Boolean.TrianglePatches;

public class TrianglePatchesTests
{
    [Fact]
    public void Build_NoIntersections_ReturnsOriginalPatches()
    {
        var triA = new Triangle(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 1));
        var triB = new Triangle(
            new Point(4, 0, 0),
            new Point(6, 0, 0),
            new Point(4, 2, 0),
            new Point(0, 0, 1));
        var set = new IntersectionSet(new[] { triA }, new[] { triB });
        var graph = global::Boolean.Intersection.Graph.Run(set);
        var index = global::Boolean.Intersection.Index.Run(graph);
        var topoA = MeshA.Run(graph, index);
        var topoB = MeshB.Run(graph, index);
        var patches = TrianglePatching.Run(graph, index, topoA, topoB);
        var aPatches = Assert.Single(patches.TrianglesA);
        var bPatches = Assert.Single(patches.TrianglesB);
        Assert.Single(aPatches);
        Assert.Single(bPatches);
        AssertAreaEqual(triA, aPatches);
        AssertAreaEqual(triB, bPatches);
    }

    [Fact]
    public void Build_SegmentIntersection_TriangulatesExpectedPieces()
    {
        var triA = new Triangle(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 1));

        var triB = new Triangle(
            new Point(1, 0, -1),
            new Point(1, 0, 1),
            new Point(1, 2, 0),
            new Point(2, 0, 0));

        var set = new IntersectionSet(new[] { triA }, new[] { triB });
        var graph = global::Boolean.Intersection.Graph.Run(set);
        var index = global::Boolean.Intersection.Index.Run(graph);
        var topoA = MeshA.Run(graph, index);
        var topoB = MeshB.Run(graph, index);
        var patches = TrianglePatching.Run(graph, index, topoA, topoB);

        var a = Assert.Single(patches.TrianglesA);
        var b = Assert.Single(patches.TrianglesB);

        // Strict counts for this configuration.
        Assert.Equal(3, a.Count);
        Assert.Equal(2, b.Count);

        // Strict geometry (set-equality of triangles, tolerant).
        var p0 = new RealPoint(1, 0, 0);
        var p1 = new RealPoint(1, 1, 0);

        var expectedA = new[]
        {
            TriKey(new RealPoint(triA.P0), p1, new RealPoint(triA.P2)),
            TriKey(new RealPoint(triA.P0), p0, p1),
            TriKey(p0, new RealPoint(triA.P1), p1),
        };

        var expectedB = new[]
        {
            TriKey(new RealPoint(triB.P0), p0, new RealPoint(triB.P2)),
            TriKey(new RealPoint(triB.P1), new RealPoint(triB.P2), p0),
        };

        AssertTriSetEqual(expectedA, a);
        AssertTriSetEqual(expectedB, b);

        AssertAreaEqual(triA, a);
        AssertAreaEqual(triB, b);
    }

    private static void AssertTriSetEqual(string[] expectedKeys, IReadOnlyList<RealTriangle> actual)
    {
        var expected = new HashSet<string>(expectedKeys);
        var got = new HashSet<string>();

        for (int i = 0; i < actual.Count; i++)
        {
            var t = actual[i];
            got.Add(TriKey(t.P0, t.P1, t.P2));
        }

        Assert.True(
            expected.SetEquals(got),
            $"Triangle set mismatch.\nExpected:\n  {string.Join("\n  ", expected)}\nGot:\n  {string.Join("\n  ", got)}");
    }

    private static string TriKey(RealPoint a, RealPoint b, RealPoint c)
    {
        // Order-insensitive within a triangle: sort vertices lexicographically after rounding.
        var p = new[] { PtKey(a), PtKey(b), PtKey(c) };
        Array.Sort(p, StringComparer.Ordinal);
        return $"{p[0]} | {p[1]} | {p[2]}";
    }

    private static string PtKey(RealPoint p)
    {
        // Epsilon-tolerant key. Tune digits if your RealPoint has more noise.
        // If you want to tie it to tolerances, replace with rounding based on Tolerances.MergeEpsilon.
        return $"{R(p.X)},{R(p.Y)},{R(p.Z)}";

        static string R(double v) => Math.Round(v, 9).ToString("G17");
    }

    private static void AssertAreaEqual(Triangle tri, IReadOnlyList<RealTriangle> patches)
    {
        double triArea = Math.Abs(new RealTriangle(
            new RealPoint(tri.P0),
            new RealPoint(tri.P1),
            new RealPoint(tri.P2)).SignedArea3D);
        double patchArea = patches.Sum(p => Math.Abs(new RealTriangle(p.P0, p.P1, p.P2).SignedArea3D));
        double diff = Math.Abs(patchArea - triArea);
        double relTol = Tolerances.BarycentricInsideEpsilon * triArea;
        Assert.True(diff <= Tolerances.EpsArea || diff <= relTol, $"Patch area {patchArea} differs from triangle area {triArea} by {diff}.");
    }
}





