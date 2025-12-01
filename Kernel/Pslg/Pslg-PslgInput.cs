using System;
using System.Collections.Generic;
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
}
