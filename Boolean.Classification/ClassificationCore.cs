using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

internal static class ClassificationCore
{
    internal static PatchClassification Run(IntersectionSet set, TrianglePatches patches)
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
                var containment = tester.Classify(in sample);
                classified[p] = new PatchInfo(patch, containment);
            }

            result[i] = classified;
        }

        return result;
    }
}
