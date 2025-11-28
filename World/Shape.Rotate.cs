using Geometry;
using Topology;

namespace World;

// Rotation-related APIs for Shape (destructive)
public abstract partial class Shape
{
    // Rotates all mesh vertices by Euler angles (degrees) around X, Y, Z (in that order)
    // using double-precision math and integer rounding. Throws if two distinct original
    // vertices collide to the same rounded grid point.
    public void Rotate(double xDegrees = 0, double yDegrees = 0, double zDegrees = 0)
    {
        if (Mesh is null || Mesh.Count == 0) return;

        // Precompute sines/cosines (radians)
        double rx = xDegrees * Math.PI / 180.0;
        double ry = yDegrees * Math.PI / 180.0;
        double rz = zDegrees * Math.PI / 180.0;
        double cx = Math.Cos(rx), sx = Math.Sin(rx);
        double cy = Math.Cos(ry), sy = Math.Sin(ry);
        double cz = Math.Cos(rz), sz = Math.Sin(rz);

        // Map unique original vertices -> rotated vertices
        var origToRot = new Dictionary<Point, Point>();
        var rotToOrig = new Dictionary<Point, Point>();

        Point MapVertex(in Point p)
        {
            if (origToRot.TryGetValue(p, out var r)) return r;

            // Apply X then Y then Z rotations using double precision
            double x = p.X, y = p.Y, z = p.Z;
            // Rotate around X
            double y1 = y * cx - z * sx;
            double z1 = y * sx + z * cx;
            double x1 = x;
            // Rotate around Y
            double x2 = x1 * cy + z1 * sy;
            double z2 = -x1 * sy + z1 * cy;
            double y2 = y1;
            // Rotate around Z
            double x3 = x2 * cz - y2 * sz;
            double y3 = x2 * sz + y2 * cz;
            double z3 = z2;

            var rot = new Point(
                (long)Math.Round(x3, MidpointRounding.AwayFromZero),
                (long)Math.Round(y3, MidpointRounding.AwayFromZero),
                (long)Math.Round(z3, MidpointRounding.AwayFromZero));

            if (rotToOrig.TryGetValue(rot, out var existing) && !existing.Equals(p))
            {
                throw new InvalidOperationException($"Rotation maps distinct vertices {existing} and {p} to the same grid point {rot}.");
            }
            rotToOrig[rot] = p;
            origToRot[p] = rot;
            return rot;
        }

        var tris = Mesh.Triangles;
        var updated = new List<Triangle>(tris.Count);
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            var a = MapVertex(t.P0);
            var b = MapVertex(t.P1);
            var c = MapVertex(t.P2);
            // Preserve winding; rigid rotation preserves orientation (rounding may throw if degenerate)
            updated.Add(Triangle.FromWinding(a, b, c));
        }

        Mesh = new Mesh(updated);
    }
}
