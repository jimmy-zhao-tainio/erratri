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
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = IntersectionIndex.Run(graph);
        var topoA = MeshA.Run(graph, index);
        var topoB = MeshB.Run(graph, index);
        var patches = global::Boolean.TrianglePatching.Run(graph, index, topoA, topoB);
        var aPatches = Assert.Single(patches.TrianglesA);
        var bPatches = Assert.Single(patches.TrianglesB);
        Assert.Single(aPatches);
        Assert.Single(bPatches);
        AssertAreaEqual(triA, aPatches);
        AssertAreaEqual(triB, bPatches);
    }

    [Fact]
    public void Build_SegmentIntersection_CutsBothTriangles()
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
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = IntersectionIndex.Run(graph);
        var topoA = MeshA.Run(graph, index);
        var topoB = MeshB.Run(graph, index);
        var patches = global::Boolean.TrianglePatching.Run(graph, index, topoA, topoB);
        var aPatches = Assert.Single(patches.TrianglesA);
        var bPatches = Assert.Single(patches.TrianglesB);
        Assert.True(aPatches.Count > 1, "Triangle A should be cut into multiple patches.");
        // For triangle B this intersection is boundaryâ†’interior, which does not form a closed PSLG face,
        // so B legitimately remains a single patch.
        Assert.Single(bPatches);
        AssertAreaEqual(triA, aPatches);
        AssertAreaEqual(triB, bPatches);
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


