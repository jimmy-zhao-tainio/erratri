using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Selects which patches to keep for a given boolean operation using
// patch-level inside/outside classification.
public static class BooleanPatchClassifier
{
    public static BooleanPatchSet Select(BooleanOperation operation, PatchClassification classification)
    {
        if (classification is null) throw new ArgumentNullException(nameof(classification));

        var keptA = new List<RealTriangle>();
        var keptB = new List<RealTriangle>();

        for (int i = 0; i < classification.MeshA.Count; i++)
        {
            foreach (var patch in classification.MeshA[i])
            {
                if (ShouldKeepFromA(operation, patch.IsInsideOtherMesh))
                {
                    keptA.Add(patch.Patch);
                }
            }
        }

        for (int i = 0; i < classification.MeshB.Count; i++)
        {
            foreach (var patch in classification.MeshB[i])
            {
                if (ShouldKeepFromB(operation, patch.IsInsideOtherMesh))
                {
                    keptB.Add(patch.Patch);
                }
            }
        }

        return new BooleanPatchSet(keptA, keptB);
    }

    private static bool ShouldKeepFromA(BooleanOperation op, bool isInsideB) => op switch
    {
        BooleanOperation.Intersection => isInsideB,
        BooleanOperation.Union => !isInsideB,
        BooleanOperation.DifferenceAB => !isInsideB,
        BooleanOperation.DifferenceBA => isInsideB,
        BooleanOperation.SymmetricDifference => !isInsideB,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.")
    };

    private static bool ShouldKeepFromB(BooleanOperation op, bool isInsideA) => op switch
    {
        BooleanOperation.Intersection => isInsideA,
        BooleanOperation.Union => !isInsideA,
        BooleanOperation.DifferenceAB => isInsideA,
        BooleanOperation.DifferenceBA => !isInsideA,
        BooleanOperation.SymmetricDifference => !isInsideA,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.")
    };
}
