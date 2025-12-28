using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

internal static class VertexQuantizer
{
    public static int AddOrGet(
        List<RealPoint> vertices,
        Dictionary<QuantizedVertexKey, int> map,
        in RealPoint point)
    {
        var key = QuantizedVertexKey.FromRealPoint(in point);

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

