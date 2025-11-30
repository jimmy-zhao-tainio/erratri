using System.Collections.Generic;
using Geometry;
using Geometry.Predicates.Internal;
using Topology;
using Xunit;

namespace Geometry.Tests;

public class PairIntersectionMathTests
{
    [Fact]
    public void ComputeNonCoplanarIntersectionPoints_SegmentCase_ReturnsTwoEndpoints()
    {
        var triA = new Triangle(
            new Point(0, -1, 0),
            new Point(0, 1, 0),
            new Point(1, 0, 0),
            new Point(0, 0, 1));

        var triB = new Triangle(
            new Point(0, 0, -1),
            new Point(0, 0, 1),
            new Point(0, 2, 0),
            new Point(1, 0, 0));

        var points = PairIntersectionMath.ComputeNonCoplanarIntersectionPoints(in triA, in triB);

        Assert.Equal(2, points.Count);

        var realTriA = new RealTriangle(triA);
        var realTriB = new RealTriangle(triB);

        foreach (var p in points)
        {
            var rp = new RealPoint(p.X, p.Y, p.Z);
            var baryA = realTriA.ComputeBarycentric(in rp, out _);
            var baryB = realTriB.ComputeBarycentric(in rp, out _);

            Assert.True(baryA.IsInsideInclusive(), "Point not inside triangle A.");
            Assert.True(baryB.IsInsideInclusive(), "Point not inside triangle B.");
        }
    }

    [Fact]
    public void ComputeCoplanarIntersectionPoints_IdenticalTriangles_ReturnsTriangleCorners()
    {
        var triA = new Triangle(
            new Point(0, 0, 0),
            new Point(6, 0, 0),
            new Point(0, 6, 0),
            new Point(0, 0, 1));

        var triB = new Triangle(
            new Point(0, 0, 0),
            new Point(6, 0, 0),
            new Point(0, 6, 0),
            new Point(0, 0, 1));

        var points = PairIntersectionMath.ComputeCoplanarIntersectionPoints(
            in triA,
            in triB);

        Assert.True(points.Count >= 3);

        var trianglePoints = new List<(long X, long Y)>
        {
            (triA.P0.X, triA.P0.Y),
            (triA.P1.X, triA.P1.Y),
            (triA.P2.X, triA.P2.Y),
        };

        bool HasCorner(long x, long y)
        {
            var plane = TriangleProjection2D.ChooseProjectionAxis(triA.Normal);
            foreach (var p in points)
            {
                // Simple projection-aware check: map the 3D grid points
                // into the same 2D space as ProjectTriangleTo2D.
                double px, py;
                if (plane == TriangleProjection2D.ProjectionPlane.YZ)
                {
                    px = y;
                    py = 0; // Z is always 0 in this test.
                }
                else if (plane == TriangleProjection2D.ProjectionPlane.XZ)
                {
                    px = x;
                    py = 0;
                }
                else
                {
                    px = x;
                    py = y;
                }

                if (System.Math.Abs(p.X - px) <= Tolerances.TrianglePredicateEpsilon &&
                    System.Math.Abs(p.Y - py) <= Tolerances.TrianglePredicateEpsilon)
                {
                    return true;
                }
            }

            return false;
        }

        foreach (var (x, y) in trianglePoints)
        {
            Assert.True(HasCorner(x, y), "Missing projected triangle corner.");
        }
    }
}
