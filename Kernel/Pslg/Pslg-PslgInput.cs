using System;
using System.Collections.Generic;
using System.Diagnostics;
using Geometry;

namespace Kernel;

public readonly struct PslgInput
{
    public Triangle Triangle { get; }
    public IReadOnlyList<TriangleSubdivision.IntersectionPoint> Points { get; }
    public IReadOnlyList<TriangleSubdivision.IntersectionSegment> Segments { get; }

    public PslgInput(
        in Triangle triangle,
        IReadOnlyList<TriangleSubdivision.IntersectionPoint> points,
        IReadOnlyList<TriangleSubdivision.IntersectionSegment> segments)
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
        if (Triangle.HasZeroArea(in p0, in p1, in p2))
        {
            throw new InvalidOperationException("Triangle must be non-degenerate.");
        }

        for (int i = 0; i < Points.Count; i++)
        {
            var p = Points[i];
            var barycentric = p.Barycentric;
            Debug.Assert(barycentric.IsInsideInclusive(), "Intersection point barycentric coordinates must lie inside the triangle.");

            var pos = p.Position;
            Debug.Assert(double.IsFinite(pos.X) && double.IsFinite(pos.Y) && double.IsFinite(pos.Z), "Intersection point position must be finite.");
        }

        for (int i = 0; i < Segments.Count; i++)
        {
            var s = Segments[i];
            Debug.Assert(s.StartIndex >= 0 && s.StartIndex < Points.Count, "Segment start index out of range.");
            Debug.Assert(s.EndIndex >= 0 && s.EndIndex < Points.Count, "Segment end index out of range.");
            Debug.Assert(s.StartIndex != s.EndIndex, "Segment endpoints must be distinct.");
        }
    }
}
