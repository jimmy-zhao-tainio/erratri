using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

// Selects which patches to keep for a given boolean operation using
// patch-level inside/outside classification.
public static class PatchSelector
{
    public static BooleanPatchSet Select(BooleanOperationType operation, PatchClassification classification)
    {
        if (classification is null) throw new ArgumentNullException(nameof(classification));

        var keptA = new List<RealTriangle>();
        var keptB = new List<RealTriangle>();

        for (int i = 0; i < classification.MeshA.Count; i++)
        {
            foreach (var patch in classification.MeshA[i])
            {
                if (ShouldKeepFromA(operation, patch.Containment))
                {
                    keptA.Add(patch.Patch);
                }
            }
        }

        for (int i = 0; i < classification.MeshB.Count; i++)
        {
            foreach (var patch in classification.MeshB[i])
            {
                if (ShouldKeepFromB(operation, patch.Containment))
                {
                    keptB.Add(patch.Patch);
                }
            }
        }

        return new BooleanPatchSet(keptA, keptB);
    }

    private static bool ShouldKeepFromA(BooleanOperationType op, Containment containment) => op switch
    {
        BooleanOperationType.Intersection => containment == Containment.Inside,
        BooleanOperationType.Union => containment == Containment.Outside,
        BooleanOperationType.DifferenceAB => containment == Containment.Outside,
        BooleanOperationType.DifferenceBA => containment == Containment.Inside,
        BooleanOperationType.SymmetricDifference => containment == Containment.Outside,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.")
    };

    private static bool ShouldKeepFromB(BooleanOperationType op, Containment containment) => op switch
    {
        BooleanOperationType.Intersection => containment == Containment.Inside,
        BooleanOperationType.Union => containment == Containment.Outside,
        BooleanOperationType.DifferenceAB => containment == Containment.Inside,
        BooleanOperationType.DifferenceBA => containment == Containment.Outside,
        BooleanOperationType.SymmetricDifference => containment == Containment.Outside,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.")
    };
}
