namespace Geometry.Topology;

public static class MeshPredicates
{
    // Returns true if every undirected edge is used by exactly two triangles.
    public static bool IsManifold(Mesh surface)
    {
        if (surface is null) throw new ArgumentNullException(nameof(surface));
        var tris = surface.Triangles;
        if (tris.Count == 0) return false;

        var edgeCounts = new Dictionary<EdgeKey, int>(tris.Count * 3);
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            CountEdge(ref edgeCounts, new EdgeKey(t.P0, t.P1));
            CountEdge(ref edgeCounts, new EdgeKey(t.P1, t.P2));
            CountEdge(ref edgeCounts, new EdgeKey(t.P2, t.P0));
        }

        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value != 2) return false;
        }
        return true;
    }

    private static void CountEdge(ref Dictionary<EdgeKey, int> map, EdgeKey e)
    {
        if (map.TryGetValue(e, out var c)) map[e] = c + 1; else map[e] = 1;
    }
}

