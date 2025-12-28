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
        IReadOnlyList<IReadOnlyList<TrianglePatch>> meshPatches,
        PointInMeshTester tester)
    {
        var result = new IReadOnlyList<PatchInfo>[meshPatches.Count];

        for (int i = 0; i < meshPatches.Count; i++)
        {
            var patches = meshPatches[i];
            var classified = new PatchInfo[patches.Count];

            if (patches.Count > 0)
            {
                var bestByFace = new Dictionary<int, int>(patches.Count);
                var bestArea = new Dictionary<int, double>(patches.Count);

                for (int p = 0; p < patches.Count; p++)
                {
                    var patch = patches[p];
                    double area = patch.Triangle.SignedArea3D;

                    if (!bestArea.TryGetValue(patch.FaceId, out var current) || area > current)
                    {
                        bestArea[patch.FaceId] = area;
                        bestByFace[patch.FaceId] = p;
                    }
                }

                var containmentByFace = new Dictionary<int, Containment>(bestByFace.Count);
                foreach (var kvp in bestByFace)
                {
                    int faceId = kvp.Key;
                    var tri = patches[kvp.Value].Triangle;
                    var sample = tri.Centroid;
                    var containment = ResolveCoplanarContainment(in tri, in sample, tester);

                    containmentByFace[faceId] = containment;
                }

                for (int p = 0; p < patches.Count; p++)
                {
                    var patch = patches[p];
                    var containment = containmentByFace[patch.FaceId];
                    classified[p] = new PatchInfo(
                        patch.Triangle,
                        patch.FaceId,
                        patch.VertexIds,
                        patch.CoplanarOwner,
                        containment);
                }
            }

            result[i] = classified;
        }

        return result;
    }

    private static Containment ResolveCoplanarContainment(
        in RealTriangle tri,
        in RealPoint sample,
        PointInMeshTester tester)
    {
        var p0 = tri.P0;
        var p1 = tri.P1;
        var p2 = tri.P2;
        var edgeA = RealVector.FromPoints(in p0, in p1);
        var edgeB = RealVector.FromPoints(in p0, in p2);
        var normal = edgeA.Cross(in edgeB);
        double len = normal.Length();
        if (len <= 0.0)
        {
            return Containment.On;
        }

        double invLen = 1.0 / len;
        var unit = new RealVector(normal.X * invLen, normal.Y * invLen, normal.Z * invLen);
        double offset = Math.Max(Tolerances.PlaneSideEpsilon * 10.0, Tolerances.PslgVertexMergeEpsilon);

        // Bias toward the inside of the source mesh (opposite the outward normal).
        var inside = new RealPoint(
            sample.X - unit.X * offset,
            sample.Y - unit.Y * offset,
            sample.Z - unit.Z * offset);

        var outside = new RealPoint(
            sample.X + unit.X * offset,
            sample.Y + unit.Y * offset,
            sample.Z + unit.Z * offset);

        var insideResult = tester.Classify(in inside);
        var outsideResult = tester.Classify(in outside);

        if (insideResult == Containment.On && outsideResult == Containment.On)
        {
            return Containment.On;
        }

        if (insideResult == Containment.On)
        {
            insideResult = outsideResult;
        }

        if (outsideResult == Containment.On)
        {
            outsideResult = insideResult;
        }

        if (insideResult == Containment.Inside && outsideResult == Containment.Outside)
        {
            return Containment.Inside;
        }

        if (insideResult == Containment.Outside && outsideResult == Containment.Inside)
        {
            return Containment.On;
        }

        return insideResult;
    }
}
