using System.Collections.Generic;
using Geometry.Topology;
using Geometry;

namespace World;

public sealed class Box : Shape
{
    public Box(long width, long depth, long height)
    {
        Width = width;
        Depth = depth;
        Height = height;

        var origin = new Point(0, 0, 0);

        var p000 = origin;
        var p100 = new Point(origin.X + Width, origin.Y, origin.Z);
        var p010 = new Point(origin.X, origin.Y + Depth, origin.Z);
        var p001 = new Point(origin.X, origin.Y, origin.Z + Height);
        var p110 = new Point(origin.X + Width, origin.Y + Depth, origin.Z);
        var p101 = new Point(origin.X + Width, origin.Y, origin.Z + Height);
        var p011 = new Point(origin.X, origin.Y + Depth, origin.Z + Height);
        var p111 = new Point(origin.X + Width, origin.Y + Depth, origin.Z + Height);

        var tetrahedra = new List<Geometry.Tetrahedron>(5)
        {
            new Geometry.Tetrahedron(p000, p100, p010, p001),
            new Geometry.Tetrahedron(p100, p110, p010, p111),
            new Geometry.Tetrahedron(p100, p010, p001, p111),
            new Geometry.Tetrahedron(p010, p001, p011, p111),
            new Geometry.Tetrahedron(p100, p001, p101, p111)
        };

        Mesh = Mesh.FromTetrahedra(tetrahedra);
    }

    public long Width { get; }
    public long Depth { get; }
    public long Height { get; }
}

