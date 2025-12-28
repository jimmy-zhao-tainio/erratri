using System.Collections.Generic;

namespace Geometry;

public static class VertexWeld
{
    public static int GetOrAddCanonicalId(
        in RealPoint point,
        double epsilon,
        List<RealPoint> idToPosition,
        Dictionary<(long X, long Y, long Z), List<int>> voxelToIds)
    {
        double inv = 1.0 / epsilon;
        double epsilonSquared = epsilon * epsilon;

        long vx = (long)System.Math.Floor(point.X * inv);
        long vy = (long)System.Math.Floor(point.Y * inv);
        long vz = (long)System.Math.Floor(point.Z * inv);

        for (long dx = -1; dx <= 1; dx++)
        for (long dy = -1; dy <= 1; dy++)
        for (long dz = -1; dz <= 1; dz++)
        {
            var key = (vx + dx, vy + dy, vz + dz);

            if (!voxelToIds.TryGetValue(key, out var candidates))
            {
                continue;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int id = candidates[i];
                var c = idToPosition[id];

                if (point.DistanceSquared(in c) <= epsilonSquared)
                {
                    return id;
                }
            }
        }

        int newId = idToPosition.Count;
        idToPosition.Add(point);

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
