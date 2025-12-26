using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Topology;

namespace Boolean;

public sealed class PatchClassification
{
    public IReadOnlyList<IReadOnlyList<PatchInfo>> MeshA { get; }
    public IReadOnlyList<IReadOnlyList<PatchInfo>> MeshB { get; }

    public PatchClassification(
        IReadOnlyList<IReadOnlyList<PatchInfo>> meshA,
        IReadOnlyList<IReadOnlyList<PatchInfo>> meshB)
    {
        MeshA = meshA ?? throw new ArgumentNullException(nameof(meshA));
        MeshB = meshB ?? throw new ArgumentNullException(nameof(meshB));
    }
}

public readonly struct PatchInfo
{
    public RealTriangle Patch { get; }
    public bool IsInsideOtherMesh { get; }

    public PatchInfo(RealTriangle patch, bool isInsideOtherMesh)
    {
        Patch = patch;
        IsInsideOtherMesh = isInsideOtherMesh;
    }
}

// Patch-level classifier: labels each patch of mesh A as inside/outside mesh B,
// and vice versa, using a robust ray-casting test against the closed mesh.
public static class Classification
{
    public static PatchClassification Run(IntersectionSet set, Patching patches)
    {
        if (patches is null) throw new ArgumentNullException(nameof(patches));

        var trianglesA = set.TrianglesA ?? throw new ArgumentNullException(nameof(set.TrianglesA));
        var trianglesB = set.TrianglesB ?? throw new ArgumentNullException(nameof(set.TrianglesB));

        var testerA = new PointInMeshTester(trianglesA);
        var testerB = new PointInMeshTester(trianglesB);

        var classifiedA = ClassifyMeshPatches(patches.TrianglesA, testerB);
        var classifiedB = ClassifyMeshPatches(patches.TrianglesB, testerA);

        return new PatchClassification(classifiedA, classifiedB);
    }

    private static IReadOnlyList<IReadOnlyList<PatchInfo>> ClassifyMeshPatches(
        IReadOnlyList<IReadOnlyList<RealTriangle>> meshPatches,
        PointInMeshTester tester)
    {
        var result = new IReadOnlyList<PatchInfo>[meshPatches.Count];

        for (int i = 0; i < meshPatches.Count; i++)
        {
            var patches = meshPatches[i];
            var classified = new PatchInfo[patches.Count];

            for (int p = 0; p < patches.Count; p++)
            {
                var patch = patches[p];
                var sample = patch.Centroid;
                bool inside = tester.Contains(sample);
                classified[p] = new PatchInfo(patch, inside);
            }

            result[i] = classified;
        }

        return result;
    }

}

