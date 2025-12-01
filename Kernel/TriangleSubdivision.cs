using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

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
//   - General PSLG lane is implemented in PslgCore but not yet invoked here;
//     non-fast patterns still throw to avoid silent fallbacks.
public static class TriangleSubdivision
{
    // Reference UV triangle has vertices (1,0), (0,1), (0,0); its area is 1/2.
    public const double ReferenceTriangleAreaUv = 0.5;

    // Location of a point on the reference triangle, expressed in terms of
    // which oriented edge it lies on (if any). The numbering matches the
    // TRIANGLESUBDIVISION roadmap:
    //
    //   - Edge0: edge V0 -> V1  (w ≈ 0)
    //   - Edge1: edge V1 -> V2  (u ≈ 0)
    //   - Edge2: edge V2 -> V0  (v ≈ 0)
    //   - Interior: strictly inside or numerically away from all edges.
    public enum EdgeLocation
    {
        Interior = 0,
        Edge0 = 1,
        Edge1 = 2,
        Edge2 = 3
    }

    // High-level classification of the intersection-segment pattern on a
    // triangle. This is a deliberately small set for the "fast lane":
    //
    //   - None: no segments.
    //   - SingleEdgeToEdge: exactly one segment whose endpoints lie on two
    //     distinct edges (no interior endpoints).
    //   - Other: everything else (to be handled by the general PSLG lane).
    public enum PatternKind
    {
        None = 0,
        SingleEdgeToEdge = 1,
        Other = 2
    }

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

    public readonly struct IntersectionPoint
    {
        public Barycentric Barycentric { get; }
        public RealPoint Position { get; }

        public IntersectionPoint(Barycentric barycentric, RealPoint position)
        {
            Barycentric = barycentric;
            Position = position;
        }
    }

    public readonly struct IntersectionSegment
    {
        public int StartIndex { get; }
        public int EndIndex { get; }

        public IntersectionSegment(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    public static IReadOnlyList<RealTriangle> Subdivide(
        in Triangle triangle,
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<IntersectionSegment> segments)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (segments is null) throw new ArgumentNullException(nameof(segments));

        // Filter out degenerate segments (same start/end); if nothing remains
        // we treat this as the trivial no-segment case.
        var filteredSegments = new List<IntersectionSegment>(segments.Count);
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.StartIndex < 0 || seg.StartIndex >= points.Count ||
                seg.EndIndex < 0 || seg.EndIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment indices must be valid point indices.");
            }

            if (seg.StartIndex == seg.EndIndex)
            {
                continue; // ignore degenerate segment
            }

            filteredSegments.Add(seg);
        }

        // Phase A: trivial case only.
        if (filteredSegments.Count == 0)
        {
            var patches = new List<RealTriangle>(capacity: 1)
            {
                new RealTriangle(
                    new RealPoint(triangle.P0),
                    new RealPoint(triangle.P1),
                    new RealPoint(triangle.P2))
            };

            return patches;
        }

        // Fast-lane patterns (Phase B) or general PSLG lane.
        var pattern = ClassifyPattern(points, filteredSegments);
        bool SegmentHasVertexEndpoint(IntersectionSegment s)
        {
            var pA = points[s.StartIndex].Barycentric;
            var pB = points[s.EndIndex].Barycentric;
            return IsAtVertex(pA) || IsAtVertex(pB);
        }

        switch (pattern)
        {
            case PatternKind.SingleEdgeToEdge when !SegmentHasVertexEndpoint(filteredSegments[0]):
                try
                {
                    return SubdivideSingleEdgeToEdge(triangle, points, filteredSegments);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("vertex endpoints", StringComparison.OrdinalIgnoreCase))
                {
                    // Fall through to PSLG path below.
                }
                goto default;

            case PatternKind.None:
                // Should not occur here because segments.Count > 0, but keep
                // a defensive sanity check.
                throw new InvalidOperationException(
                    "TriangleSubdivision.Subdivide: Pattern.None with non-empty segment list.");

            default:
                // General PSLG-based subdivision path: run the full PSLG pipeline
                // for this triangle and use the resulting patches.
                var pslgInput = new PslgInput(in triangle, points, filteredSegments);
                var pslgResult = PslgBuilder.Run(in pslgInput);
                return pslgResult.Patches;
        }
    }

    /// <summary>
    /// Fast-path subdivision for a single interior chord whose endpoints lie
    /// on two distinct adjacent triangle edges.
    ///
    /// Preconditions:
    ///   - Exactly one segment is provided.
    ///   - Both endpoints lie on triangle edges (not in the interior).
    ///   - Endpoints lie on two distinct edges that are adjacent in the
    ///     oriented edge cycle 0-&gt;1-&gt;2-&gt;0.
    ///
    /// This method does NOT handle:
    ///   - Interior endpoints,
    ///   - Endpoints on the same edge,
    ///   - Endpoints at (or very close to) triangle vertices,
    ///   - Multiple segments or junctions.
    ///
    /// Unsupported configurations must be handled by the generic PSLG-based
    /// subdivision path, not widened here. For valid inputs this returns
    /// exactly three patches whose union reconstructs the original triangle
    /// and for which the chord appears as patch edges.
    /// </summary>
    private static IReadOnlyList<RealTriangle> SubdivideSingleEdgeToEdge(
        in Triangle triangle,
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<IntersectionSegment> segments)
    {
        if (segments.Count != 1)
        {
            throw new ArgumentException("SingleEdgeToEdge subdivision requires exactly one segment.", nameof(segments));
        }

        var seg = segments[0];
        if (seg.StartIndex < 0 || seg.StartIndex >= points.Count ||
            seg.EndIndex < 0 || seg.EndIndex >= points.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(segments), "Segment indices must be valid point indices.");
        }

        var pA = points[seg.StartIndex];
        var pB = points[seg.EndIndex];

        // Reject endpoints that are (numerically) at triangle vertices. This
        // fast path is intended for interior chords; vertex endpoints should
        // be handled by the general PSLG subdivision instead.
        if (IsAtVertex(pA.Barycentric) ||
            IsAtVertex(pB.Barycentric))
        {
            throw new InvalidOperationException(
                "SubdivideSingleEdgeToEdge does not support vertex endpoints; use the PSLG path.");
        }

        var locA = ClassifyEdge(pA.Barycentric);
        var locB = ClassifyEdge(pB.Barycentric);

        if (locA == EdgeLocation.Interior || locB == EdgeLocation.Interior || locA == locB)
        {
            throw new InvalidOperationException(
                "SubdivideSingleEdgeToEdge requires both endpoints to lie on distinct triangle edges.");
        }

        // Map EdgeLocation to integer edge index 0,1,2.
        static int EdgeIndex(EdgeLocation loc) => loc switch
        {
            EdgeLocation.Edge0 => 0,
            EdgeLocation.Edge1 => 1,
            EdgeLocation.Edge2 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(loc), "Unsupported edge location.")
        };

        int edgeA = EdgeIndex(locA);
        int edgeB = EdgeIndex(locB);

        var baryP = pA.Barycentric;
        var baryQ = pB.Barycentric;

        // Canonicalize order so that edgeQ is the next edge after edgeP
        // in the oriented cycle 0->1->2->0. If not, swap endpoints.
        if (edgeB != ((edgeA + 1) % 3))
        {
            if (edgeA == ((edgeB + 1) % 3))
            {
                (edgeA, edgeB) = (edgeB, edgeA);
                (baryP, baryQ) = (baryQ, baryP);
            }
            else
            {
                throw new InvalidOperationException(
                    "SubdivideSingleEdgeToEdge expects endpoints on two edges that are adjacent in the edge cycle.");
            }
        }

        var v0 = new RealPoint(triangle.P0);
        var v1 = new RealPoint(triangle.P1);
        var v2 = new RealPoint(triangle.P2);

        var rp = Barycentric.ToRealPointOnTriangle(in triangle, in baryP);
        var rq = Barycentric.ToRealPointOnTriangle(in triangle, in baryQ);

        var patches = new List<RealTriangle>(3);

        if (edgeA == 0 && edgeB == 1)
        {
            // P on edge V0-V1, Q on edge V1-V2.
            // Triangle: rp, v1, rq
            // Quadrilateral: rq, v2, v0, rp triangulated as:
            //   rq, v2, v0 and rq, v0, rp
            patches.Add(new RealTriangle(rp, v1, rq));
            patches.Add(new RealTriangle(rq, v2, v0));
            patches.Add(new RealTriangle(rq, v0, rp));
        }
        else if (edgeA == 1 && edgeB == 2)
        {
            // P on edge V1-V2, Q on edge V2-V0.
            // Triangle: rp, v2, rq
            // Quadrilateral: rq, v0, v1, rp triangulated as:
            //   rq, v0, v1 and rq, v1, rp
            patches.Add(new RealTriangle(rp, v2, rq));
            patches.Add(new RealTriangle(rq, v0, v1));
            patches.Add(new RealTriangle(rq, v1, rp));
        }
        else if (edgeA == 2 && edgeB == 0)
        {
            // P on edge V2-V0, Q on edge V0-V1.
            // Triangle: rp, v0, rq
            // Quadrilateral: rq, v1, v2, rp triangulated as:
            //   rq, v1, v2 and rq, v2, rp
            patches.Add(new RealTriangle(rp, v0, rq));
            patches.Add(new RealTriangle(rq, v1, v2));
            patches.Add(new RealTriangle(rq, v2, rp));
        }
        else
        {
            throw new InvalidOperationException(
                "SubdivideSingleEdgeToEdge reached an unsupported edge pair after canonicalization.");
        }

        return patches;
    }

    private static bool IsAtVertex(Barycentric barycentric)
    {
        var u = barycentric.U;
        var v = barycentric.V;
        var w = barycentric.W;

        bool atV0 = Math.Abs(u - 1.0) <= Tolerances.EpsVertex &&
                    Math.Abs(v) <= Tolerances.EpsVertex &&
                    Math.Abs(w) <= Tolerances.EpsVertex;

        bool atV1 = Math.Abs(v - 1.0) <= Tolerances.EpsVertex &&
                    Math.Abs(u) <= Tolerances.EpsVertex &&
                    Math.Abs(w) <= Tolerances.EpsVertex;

        bool atV2 = Math.Abs(w - 1.0) <= Tolerances.EpsVertex &&
                    Math.Abs(u) <= Tolerances.EpsVertex &&
                    Math.Abs(v) <= Tolerances.EpsVertex;

        return atV0 || atV1 || atV2;
    }
}
