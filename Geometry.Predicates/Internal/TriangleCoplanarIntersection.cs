using System.Collections.Generic;

namespace Geometry.Predicates.Internal;

internal static class TriangleCoplanarIntersection
{
    internal static TriangleIntersection Classify(in Triangle a, in Triangle b)
    {
        // Project both triangles to 2D (dropping the dominant normal axis),
        // then build the set of candidate intersection points:
        // - vertices of A inside B
        // - vertices of B inside A
        // - edge/edge intersection points
        // Then decide intersection dimension from these candidates.

        var plane = TriangleProjection2D.ChooseProjectionAxis(a.Normal);

        var a0 = TriangleProjection2D.ProjectPointTo2D(a.P0, plane);
        var a1 = TriangleProjection2D.ProjectPointTo2D(a.P1, plane);
        var a2 = TriangleProjection2D.ProjectPointTo2D(a.P2, plane);

        var b0 = TriangleProjection2D.ProjectPointTo2D(b.P0, plane);
        var b1 = TriangleProjection2D.ProjectPointTo2D(b.P1, plane);
        var b2 = TriangleProjection2D.ProjectPointTo2D(b.P2, plane);

        var candidates = new List<TriangleProjection2D.Point2D>(12);

        // Vertices of A inside B (including on boundary)
        TriangleProjection2D.AddIfInsideTriangle(a0, b0, b1, b2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(a1, b0, b1, b2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(a2, b0, b1, b2, candidates);

        // Vertices of B inside A
        TriangleProjection2D.AddIfInsideTriangle(b0, a0, a1, a2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(b1, a0, a1, a2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(b2, a0, a1, a2, candidates);

        // Edge-edge intersections
        var aVerts = new[] { a0, a1, a2 };
        var bVerts = new[] { b0, b1, b2 };
        for (int i = 0; i < 3; i++)
        {
            var aStart = aVerts[i];
            var aEnd = aVerts[(i + 1) % 3];
            for (int j = 0; j < 3; j++)
            {
                var bStart = bVerts[j];
                var bEnd = bVerts[(j + 1) % 3];
                if (TriangleProjection2D.TrySegmentIntersection(aStart, aEnd, bStart, bEnd, out var intersection))
                {
                    TriangleProjection2D.AddUnique(candidates, intersection);
                }
            }
        }

        if (candidates.Count == 0)
            return new TriangleIntersection(TriangleIntersectionType.None);

        if (TriangleProjection2D.HasNonCollinearTriple(candidates))
        {
            // 2D overlap region has positive area.
            return new TriangleIntersection(TriangleIntersectionType.Area);
        }

        // Intersection is 0D or 1D (point or segment). Decide based on how
        // many unique candidate points we have.
        if (candidates.Count == 1)
        {
            return new TriangleIntersection(TriangleIntersectionType.Point);
        }

        return new TriangleIntersection(TriangleIntersectionType.Segment);
    }
}

