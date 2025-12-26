using Geometry.Topology;
using Geometry;

namespace World;

public sealed class Tetrahedron : Shape
{
    public Tetrahedron(Point a, Point b, Point c, Point d)
    {
        A = a; B = b; C = c; D = d;
        var tetrahedron = new Geometry.Tetrahedron(A, B, C, D);
        Mesh = Mesh.FromTetrahedra(new[] { tetrahedron });
    }

    public Point A { get; }
    public Point B { get; }
    public Point C { get; }
    public Point D { get; }
}

