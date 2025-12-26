using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

internal static class VertexQuantizer
{
    public static int AddOrGet(
        List<RealPoint> vertices,
        Dictionary<(long X, long Y, long Z), int> map,
        in RealPoint point)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;
        double inv = 1.0 / epsilon;

        long qx = (long)Math.Round(point.X * inv);
        long qy = (long)Math.Round(point.Y * inv);
        long qz = (long)Math.Round(point.Z * inv);

        var key = (qx, qy, qz);

        if (map.TryGetValue(key, out int existing))
        {
            return existing;
        }

        int idx = vertices.Count;
        vertices.Add(point);
        map[key] = idx;
        return idx;
    }
}

