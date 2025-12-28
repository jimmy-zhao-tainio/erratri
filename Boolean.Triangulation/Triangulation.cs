using System;
using System.Collections.Generic;
using Geometry;
using Pslg;

namespace Boolean;

public static partial class Triangulation
{
    public static TriangulationResult Run(
        in Triangle triangle,
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<IntersectionSegment> segments)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (segments is null) throw new ArgumentNullException(nameof(segments));

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
                continue;
            }

            filteredSegments.Add(seg);
        }

        if (filteredSegments.Count == 0)
        {
            var patches = new List<RealTriangle>(capacity: 1)
            {
                new RealTriangle(
                    new RealPoint(triangle.P0),
                    new RealPoint(triangle.P1),
                    new RealPoint(triangle.P2))
            };

            return new TriangulationResult(patches, new[] { 0 });
        }

        var pattern = ClassifyPattern(points, filteredSegments);

        switch (pattern)
        {
            case PatternKind.SingleEdgeToEdge when !SegmentHasVertexEndpoint(points, filteredSegments[0]):
                try
                {
                    var patches = SingleEdgeToEdge.Triangulate(triangle, points, filteredSegments);
                    return new TriangulationResult(patches, new int[patches.Count]);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("vertex endpoints", StringComparison.OrdinalIgnoreCase))
                {
                }
                goto default;

            case PatternKind.None:
                throw new InvalidOperationException(
                    "TriangleSubdivision.Subdivide: Pattern.None with non-empty segment list.");

            default:
                var pslgPoints = new List<PslgPoint>(points.Count);
                for (int i = 0; i < points.Count; i++)
                {
                    pslgPoints.Add(new PslgPoint(points[i].Barycentric));
                }

                var pslgSegments = new List<PslgSegment>(filteredSegments.Count);
                for (int i = 0; i < filteredSegments.Count; i++)
                {
                    var seg = filteredSegments[i];
                    pslgSegments.Add(new PslgSegment(seg.StartIndex, seg.EndIndex));
                }

                var pslgInput = new PslgInput(in triangle, pslgPoints, pslgSegments);
                var pslgOutput = PslgBuilder.Run(in pslgInput);
                return PslgToTriangles.TriangulateWithFaceIds(in triangle, pslgOutput);
        }
    }

    private static bool SegmentHasVertexEndpoint(
        IReadOnlyList<IntersectionPoint> points,
        IntersectionSegment segment)
    {
        var pA = points[segment.StartIndex].Barycentric;
        var pB = points[segment.EndIndex].Barycentric;
        return IsAtVertex(pA) || IsAtVertex(pB);
    }

    internal static bool IsAtVertex(Barycentric barycentric)
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
