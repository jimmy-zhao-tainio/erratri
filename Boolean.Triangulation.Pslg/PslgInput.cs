using System;
using System.Collections.Generic;
using System.Diagnostics;
using Geometry;

namespace Pslg;

public readonly struct PslgInput
{
    public Triangle Triangle { get; }
    public IReadOnlyList<PslgPoint> Points { get; }
    public IReadOnlyList<PslgSegment> Segments { get; }

    public PslgInput(
        in Triangle triangle,
        IReadOnlyList<PslgPoint> points,
        IReadOnlyList<PslgSegment> segments)
    {
        Triangle = triangle;
        Points = points ?? throw new ArgumentNullException(nameof(points));
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
    }

    /// <summary>
    /// Validate basic structural and geometric preconditions for PSLG input.
    /// Throws on gross misuse (nulls, degenerate triangle) and uses Debug.Assert
    /// for deeper invariants such as point placement and finite coordinates.
    /// </summary>
    internal void Validate()
    {
        if (Points is null) throw new ArgumentNullException(nameof(Points));
        if (Segments is null) throw new ArgumentNullException(nameof(Segments));

        var p0 = Triangle.P0;
        var p1 = Triangle.P1;
        var p2 = Triangle.P2;
        if (HasZeroArea(in p0, in p1, in p2))
        {
            throw new InvalidOperationException("Triangle must be non-degenerate.");
        }

        for (int i = 0; i < Points.Count; i++)
        {
            var p = Points[i];
            var barycentric = p.Barycentric;
            Debug.Assert(barycentric.IsInsideInclusive(), "Intersection point barycentric coordinates must lie inside the triangle.");
            Debug.Assert(double.IsFinite(barycentric.U) &&
                         double.IsFinite(barycentric.V) &&
                         double.IsFinite(barycentric.W), "Intersection point barycentric coordinates must be finite.");
        }

        for (int i = 0; i < Segments.Count; i++)
        {
            var s = Segments[i];
            Debug.Assert(s.StartIndex >= 0 && s.StartIndex < Points.Count, "Segment start index out of range.");
            Debug.Assert(s.EndIndex >= 0 && s.EndIndex < Points.Count, "Segment end index out of range.");
            Debug.Assert(s.StartIndex != s.EndIndex, "Segment endpoints must be distinct.");
        }
    }

    private static bool HasZeroArea(in Point p0, in Point p1, in Point p2)
    {
        long v0x = p1.X - p0.X;
        long v0y = p1.Y - p0.Y;
        long v0z = p1.Z - p0.Z;

        long v1x = p2.X - p0.X;
        long v1y = p2.Y - p0.Y;
        long v1z = p2.Z - p0.Z;

        long cx = v0y * v1z - v0z * v1y;
        long cy = v0z * v1x - v0x * v1z;
        long cz = v0x * v1y - v0y * v1x;

        long lenSq = cx * cx + cy * cy + cz * cz;
        return lenSq == 0;
    }
}
