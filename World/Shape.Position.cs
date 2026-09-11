using Geometry;
using Geometry.Topology;

namespace World;

// Translation APIs modify the shape's mesh in place.
public abstract partial class Shape
{
    /// <summary>Compatibility alias for Translate; offsets the current mesh, not an absolute position.</summary>
    public Shape Position(long dx, long dy, long dz) => Translate(dx, dy, dz);

    /// <summary>Offsets all mesh vertices by the given delta in place and returns this shape.</summary>
    public Shape Translate(long dx, long dy, long dz)
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

