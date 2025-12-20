using System;
using System.Collections.Generic;
using Geometry;

namespace Pslg.Phases;

internal static class PslgSelectionPhase
{
    // Phase #4: filter out degenerate faces, deduplicate boundaries, and select interior faces with area checks.
    internal static PslgSelectionState Run(PslgFaceState faceState)
    {
        if (faceState.Faces is null) throw new ArgumentNullException(nameof(faceState));
        var faces = faceState.Faces;
        var interiorFaces = SelectInteriorFaces(faces);

        const double ReferenceTriangleAreaUv = 0.5;
        double targetArea = Math.Abs(ReferenceTriangleAreaUv);
        double relTol = Tolerances.BarycentricInsideEpsilon * targetArea;

        // Drop any full-triangle rings when we already have multiple faces.
        IReadOnlyList<PslgFace> filtered = interiorFaces;
        if (interiorFaces.Count > 1)
        {
            var pruned = new List<PslgFace>(interiorFaces.Count);
            for (int i = 0; i < interiorFaces.Count; i++)
            {
                double areaAbs = Math.Abs(interiorFaces[i].SignedAreaUV);
                double diff = Math.Abs(areaAbs - targetArea);
                if (diff <= Tolerances.EpsArea || diff <= relTol)
                {
                    continue;
                }
                pruned.Add(interiorFaces[i]);
            }

            filtered = pruned;
        }

        if (filtered.Count == 0 && interiorFaces.Count > 0)
        {
            int maxIdx = 0;
            double maxArea = Math.Abs(interiorFaces[0].SignedAreaUV);
            for (int i = 1; i < interiorFaces.Count; i++)
            {
                double areaAbs = Math.Abs(interiorFaces[i].SignedAreaUV);
                if (areaAbs > maxArea)
                {
                    maxArea = areaAbs;
                    maxIdx = i;
                }
            }

            filtered = new List<PslgFace>(capacity: 1) { interiorFaces[maxIdx] };
        }

        double totalAbs = 0.0;
        for (int i = 0; i < filtered.Count; i++)
        {
            totalAbs += Math.Abs(filtered[i].SignedAreaUV);
        }

        double absDiff = Math.Abs(totalAbs - targetArea);
        if (absDiff > Tolerances.EpsArea && absDiff > relTol)
        {
            throw new InvalidOperationException(
                $"Face areas do not sum to the expected reference triangle area: totalAbs={totalAbs}, expected={ReferenceTriangleAreaUv}.");
        }

        return new PslgSelectionState(faceState.Vertices, faceState.Edges, faceState.HalfEdges, faces, new PslgFaceSelection(filtered));
    }

    private static List<PslgFace> SelectInteriorFaces(
        IReadOnlyList<PslgFace> faces)
    {
        if (faces is null) throw new ArgumentNullException(nameof(faces));
        if (faces.Count == 0) return new List<PslgFace>();

        var filtered = new List<PslgFace>(faces.Count);
        for (int i = 0; i < faces.Count; i++)
        {
            double areaAbs = Math.Abs(faces[i].SignedAreaUV);
            if (areaAbs <= Tolerances.EpsArea)
            {
                continue;
            }
            filtered.Add(faces[i]);
        }

        return DeduplicateFaces(filtered);
    }

    private static List<PslgFace> DeduplicateFaces(IReadOnlyList<PslgFace> faces)
    {
        var unique = new List<PslgFace>(faces.Count);
        var seen = new HashSet<string>();

        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            var key = CanonicalFaceKey(face.OuterVertices);
            if (seen.Add(key))
            {
                unique.Add(face);
            }
        }

        return unique;
    }

    internal static string CanonicalFaceKey(int[] vertices)
    {
        if (vertices is null || vertices.Length == 0)
        {
            return string.Empty;
        }

        int n = vertices.Length;
        int bestStart = 0;

        for (int start = 1; start < n; start++)
        {
            bool better = false;
            for (int k = 0; k < n; k++)
            {
                int a = vertices[(start + k) % n];
                int b = vertices[(bestStart + k) % n];
                if (a == b)
                {
                    continue;
                }

                if (a < b)
                {
                    better = true;
                }

                break;
            }

            if (better)
            {
                bestStart = start;
            }
        }

        var ordered = new int[n];
        for (int i = 0; i < n; i++)
        {
            ordered[i] = vertices[(bestStart + i) % n];
        }

        return string.Join(",", ordered);
    }
}
