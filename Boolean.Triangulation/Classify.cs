using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

// Standalone triangle subdivision helper library.
//
// Mission (as in TRIANGLESUBDIVISION-ROADMAP.md):
//   - Input: one triangle + intersection points on it + segments between those points.
//   - Output: a set of TrianglePatch triangles whose union exactly covers the
//     original triangle and whose edges align with the intersection segments.
//
// Current implementation:
//   - If there are no segments, return the original triangle as a single patch.
//   - Fast lane: single edge-to-edge chord is implemented.
//   - General PSLG lane uses the Pslg pipeline and TriangleSubdivisionTriangulator.
public static partial class Triangulation
{
    // Reference UV triangle has vertices (1,0), (0,1), (0,0); its area is 1/2.
    public const double ReferenceTriangleAreaUv = 0.5;

    // Classify a barycentric point (u, v, w) as lying on one of the three
    // triangle edges or strictly in the interior. "On edge" is interpreted
    // as having the corresponding barycentric component within |value| <= eps
    // of zero. Sum-to-one and non-negativity constraints are assumed to be
    // enforced by upstream code.
    public static EdgeLocation ClassifyEdge(Barycentric barycentric)
    {
        var u = barycentric.U;
        var v = barycentric.V;
        var w = barycentric.W;

        if (Math.Abs(w) <= Tolerances.EpsVertex) return EdgeLocation.Edge0; // V0-V1
        if (Math.Abs(u) <= Tolerances.EpsVertex) return EdgeLocation.Edge1; // V1-V2
        if (Math.Abs(v) <= Tolerances.EpsVertex) return EdgeLocation.Edge2; // V2-V0

        return EdgeLocation.Interior;
    }

    // Compute a simple pattern classification for the segments on this
    // triangle, using the EdgeLocation of their endpoints.
    //
    // Rules:
    //   - No segments       => PatternKind.None.
    //   - Exactly one segment:
    //       * Both endpoints on edges (not Interior), and
    //       * endpoints lie on different edges
    //     => PatternKind.SingleEdgeToEdge, otherwise PatternKind.Other.
    //   - More than one segment => PatternKind.Other.
    public static PatternKind ClassifyPattern(
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<IntersectionSegment> segments)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (segments is null) throw new ArgumentNullException(nameof(segments));

        if (segments.Count == 0)
        {
            return PatternKind.None;
        }

        if (segments.Count == 1)
        {
            var seg = segments[0];
            if (seg.StartIndex < 0 || seg.StartIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment start index is out of range.");
            }

            if (seg.EndIndex < 0 || seg.EndIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment end index is out of range.");
            }

            var pA = points[seg.StartIndex];
            var pB = points[seg.EndIndex];

            var edgeA = ClassifyEdge(pA.Barycentric);
            var edgeB = ClassifyEdge(pB.Barycentric);

            if (edgeA != EdgeLocation.Interior &&
                edgeB != EdgeLocation.Interior &&
                edgeA != edgeB)
            {
                return PatternKind.SingleEdgeToEdge;
            }

            return PatternKind.Other;
        }

        // Multiple segments: not part of the fast lane for now.
        return PatternKind.Other;
    }

}


