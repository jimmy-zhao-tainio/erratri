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

        if (filteredSegments.Count == 0 && points.Count == 0)
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

        var pslgPoints = new List<PslgPoint>(points.Count);
        foreach (var point in points) pslgPoints.Add(new PslgPoint(point.Barycentric));
        var pslgSegments = new List<PslgSegment>(filteredSegments.Count);
        foreach (var segment in filteredSegments)
            pslgSegments.Add(new PslgSegment(segment.StartIndex, segment.EndIndex));
        var input = new PslgInput(in triangle, pslgPoints, pslgSegments);
        return PslgToTriangles.TriangulateWithFaceIds(in triangle, PslgBuilder.Run(in input));
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