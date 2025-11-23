using System.Collections.Generic;
using Geometry;

namespace Geometry.Predicates;

public static class RealPolygonPredicates
{
    public static bool ContainsInclusive(RealPolygon polygon, RealPoint p)
    {
        var verts = polygon.Vertices;
        bool inside = false;
        for (int i = 0, j = verts.Count - 1; i < verts.Count; j = i++)
        {
            var vi = verts[i];
            var vj = verts[j];

            if (RealSegmentPredicates.PointOnSegment(p, new RealSegment(vj, vi)))
            {
                return true;
            }

            double dy = vj.Y - vi.Y;
            double denom = dy;
            if (Math.Abs(denom) < Tolerances.EpsVertex)
            {
                denom = dy >= 0 ? Tolerances.EpsVertex : -Tolerances.EpsVertex;
            }

            bool intersect = ((vi.Y > p.Y) != (vj.Y > p.Y)) &&
                             (p.X <= (vj.X - vi.X) * (p.Y - vi.Y) / denom + vi.X);
            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
