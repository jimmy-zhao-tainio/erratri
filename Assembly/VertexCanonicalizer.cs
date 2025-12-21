using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

internal static class VertexCanonicalizer
{
    public static int GetOrAddCanonicalId(
        RealPoint p,
        Dictionary<int, RealPoint> idToPosition,
        Dictionary<(int, int, int), List<int>> voxelToIds)
    {
        double inv = 1.0 / Tolerances.MergeEpsilon;
        int vx = (int)Math.Floor(p.X * inv);
        int vy = (int)Math.Floor(p.Y * inv);
        int vz = (int)Math.Floor(p.Z * inv);

        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dz = -1; dz <= 1; dz++)
        {
            var key = (vx + dx, vy + dy, vz + dz);

            if (!voxelToIds.TryGetValue(key, out var candidates))
            {
                continue;
            }

            foreach (var id in candidates)
            {
                var c = idToPosition[id];

                if (p.DistanceSquared(in c) <= Tolerances.MergeEpsilonSquared)
                {
                    return id;
                }
            }
        }

        int newId = idToPosition.Count;
        idToPosition[newId] = p;

        var home = (vx, vy, vz);

        if (!voxelToIds.TryGetValue(home, out var list))
        {
            list = new List<int>();
            voxelToIds[home] = list;
        }

        list.Add(newId);
        return newId;
    }
}

