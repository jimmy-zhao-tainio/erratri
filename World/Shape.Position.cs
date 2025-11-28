using Geometry;
using Topology;

namespace World;

// Positioning-related APIs for Shape (destructive)
public abstract partial class Shape
{
    // Translates all mesh vertices by the given delta and rebuilds the mesh.
    public Shape Position(long dx, long dy, long dz)
    {
        if (Mesh is null || Mesh.Count == 0) return this;

        var tris = Mesh.Triangles;
        var vertexMap = new Dictionary<Point, Point>();

        Point Map(Point p)
        {
            if (vertexMap.TryGetValue(p, out var q)) return q;
            var t = new Point(p.X + dx, p.Y + dy, p.Z + dz);
            vertexMap[p] = t;
            return t;
        }

        var updated = new List<Triangle>(tris.Count);
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            var p0 = Map(t.P0);
            var p1 = Map(t.P1);
            var p2 = Map(t.P2);
            // Preserve winding; translation cannot introduce degeneracy
            updated.Add(Triangle.FromWinding(p0, p1, p2));
        }

        Mesh = new Mesh(updated);
        return this;
    }
}
