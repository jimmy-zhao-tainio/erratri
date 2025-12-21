using System.Collections.Generic;
using Geometry;
using Boolean;
using Topology;
using Xunit;
using WTetrahedron = World.Tetrahedron;

namespace Tests.Boolean;

public class BooleanTouchingSolidsTests
{
    [Fact]
    public void TetraTouchingAtFace_IntersectionIsEmpty()
    {
        var a = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        var b = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, -2));

        var mesh = global::Boolean.Operation.Intersection(a.Mesh, b.Mesh);
        Assert.Empty(mesh.Triangles);
    }

    [Fact]
    public void TetraTouchingAtFace_UnionIsManifold()
    {
        var a = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        var b = new WTetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, -2));

        var mesh = global::Boolean.Operation.Union(a.Mesh, b.Mesh);
        Assert.NotEmpty(mesh.Triangles);
    }

    [Fact]
    public void BoxTouchingAtFace_IntersectionIsEmpty()
    {
        var boxA = MakeBoxMesh(new Point(0, 0, 0), width: 10, depth: 10, height: 10);
        var boxB = MakeBoxMesh(new Point(10, 0, 0), width: 10, depth: 10, height: 10);

        var mesh = global::Boolean.Operation.Intersection(boxA, boxB);
        Assert.Empty(mesh.Triangles);
    }

    [Fact]
    public void BoxSeparated_IntersectionIsEmpty()
    {
        var boxA = MakeBoxMesh(new Point(0, 0, 0), width: 10, depth: 10, height: 10);
        var boxB = MakeBoxMesh(new Point(100, 0, 0), width: 10, depth: 10, height: 10);

        var mesh = global::Boolean.Operation.Intersection(boxA, boxB);
        Assert.Empty(mesh.Triangles);
    }

    private static Mesh MakeBoxMesh(Point origin, long width, long depth, long height)
    {
        var p000 = origin;
        var p100 = new Point(origin.X + width, origin.Y, origin.Z);
        var p010 = new Point(origin.X, origin.Y + depth, origin.Z);
        var p001 = new Point(origin.X, origin.Y, origin.Z + height);
        var p110 = new Point(origin.X + width, origin.Y + depth, origin.Z);
        var p101 = new Point(origin.X + width, origin.Y, origin.Z + height);
        var p011 = new Point(origin.X, origin.Y + depth, origin.Z + height);
        var p111 = new Point(origin.X + width, origin.Y + depth, origin.Z + height);

        var tetrahedra = new List<Geometry.Tetrahedron>(5)
        {
            new Geometry.Tetrahedron(p000, p100, p010, p001),
            new Geometry.Tetrahedron(p100, p110, p010, p111),
            new Geometry.Tetrahedron(p100, p010, p001, p111),
            new Geometry.Tetrahedron(p010, p001, p011, p111),
            new Geometry.Tetrahedron(p100, p001, p101, p111)
        };

        return Mesh.FromTetrahedra(tetrahedra);
    }
}


