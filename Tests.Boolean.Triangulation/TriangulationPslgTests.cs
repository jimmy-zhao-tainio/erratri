using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Boolean;
using Xunit;
using TriangulationLib = global::Boolean.Triangulation;

namespace Tests.Boolean.Triangulation;

public class TriangulationPslgTests
{
    [Fact]
    public void Subdivide_OnEdgeCollinearPoints_PreservesAreaAndCorners()
    {
        // Points lie on the V1-V2 edge (u = 0), splitting that side multiple times.
        var triangle = Triangle.FromWinding(
            new Point(0, 0, 0),
            new Point(1, 0, 0),
            new Point(0, 1, 0));

        var points = new List<IntersectionPoint>
        {
            MakeOnEdgePoint(triangle, 0.7233333333333334),
            MakeOnEdgePoint(triangle, 0.8333333333333334),
            MakeOnEdgePoint(triangle, 0.16666666666666669),
        };

        // Force the PSLG path by adding a segment along that same edge.
        var segments = new List<IntersectionSegment>
        {
            new(startIndex: 0, endIndex: 2)
        };

        var patches = TriangulationLib.Run(in triangle, points, segments);

        Assert.NotEmpty(patches);
        AssertAreaEqual(triangle, patches);

        Assert.True(ContainsVertex(patches, new RealPoint(triangle.P0)));
        Assert.True(ContainsVertex(patches, new RealPoint(triangle.P1)));
        Assert.True(ContainsVertex(patches, new RealPoint(triangle.P2)));
    }

    [Fact]
    public void Subdivide_CollinearEdgeSubdivisions_PreservesArea()
    {
        var triangle = Triangle.FromWinding(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0));

        var points = new List<IntersectionPoint>
        {
            MakeOnEdgePoint(triangle, 0.2),
            MakeOnEdgePoint(triangle, 0.5),
            MakeOnEdgePoint(triangle, 0.8),
            MakeOnEdgePoint(triangle, 0.9)
        };

        var segments = new List<IntersectionSegment>
        {
            new(0, 1),
            new(1, 2),
            new(2, 3)
        };

        var patches = TriangulationLib.Run(in triangle, points, segments);

        Assert.NotEmpty(patches);
        AssertAreaEqual(triangle, patches);
    }

    private static IntersectionPoint MakeOnEdgePoint(
        Triangle triangle,
        double vOnEdge)
    {
        var bary = new Barycentric(0.0, vOnEdge, 1.0 - vOnEdge);
        var pos = Barycentric.ToRealPointOnTriangle(in triangle, in bary);
        return new IntersectionPoint(bary, pos);
    }

    private static void AssertAreaEqual(Triangle tri, IReadOnlyList<RealTriangle> patches)
    {
        double triArea = Math.Abs(new RealTriangle(
            new RealPoint(tri.P0),
            new RealPoint(tri.P1),
            new RealPoint(tri.P2)).SignedArea3D);

        double patchArea = patches.Sum(p => Math.Abs(new RealTriangle(p.P0, p.P1, p.P2).SignedArea3D));

        double diff = Math.Abs(patchArea - triArea);
        double relTol = Tolerances.BarycentricInsideEpsilon * triArea;

        Assert.True(
            diff <= Tolerances.EpsArea || diff <= relTol,
            $"Patch area {patchArea} differs from triangle area {triArea} by {diff}.");
    }

    private static bool ContainsVertex(
        IReadOnlyList<RealTriangle> patches,
        RealPoint vertex,
        double tol = 1e-6)
    {
        for (int i = 0; i < patches.Count; i++)
        {
            var p = patches[i];
            if (SamePoint(p.P0, vertex, tol) ||
                SamePoint(p.P1, vertex, tol) ||
                SamePoint(p.P2, vertex, tol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SamePoint(RealPoint p, RealPoint q, double tol)
    {
        double dx = p.X - q.X;
        double dy = p.Y - q.Y;
        double dz = p.Z - q.Z;
        double d2 = dx * dx + dy * dy + dz * dz;
        return d2 <= tol * tol;
    }
}




