using System.Collections.Generic;
using Geometry;

namespace Geometry.Predicates;

public static class RealPolygonPredicates
{
    public static bool ContainsInclusive(RealPolygon polygon, RealPoint p)
    {
        var vertices = polygon.Vertices;
        bool inside = false;
        for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
        {
            var vi = vertices[i];
            var vj = vertices[j];

            if (RealSegmentPredicates.PointOnSegment(p, new RealSegment(vj, vi)))
            {
                return true;
            }

            double dy = vj.Y - vi.Y;
            double denominator = dy;
            if (Math.Abs(denominator) < Tolerances.EpsVertex)
            {
                denominator = dy >= 0 ? Tolerances.EpsVertex : -Tolerances.EpsVertex;
            }

            bool intersect = ((vi.Y > p.Y) != (vj.Y > p.Y)) &&
                             (p.X <= (vj.X - vi.X) * (p.Y - vi.Y) / denominator + vi.X);
            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
