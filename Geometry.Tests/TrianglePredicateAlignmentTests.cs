using Geometry;
using Geometry.Predicates.Internal;
using Xunit;

namespace Geometry.Tests;

public class TrianglePredicateAlignmentTests
{
    private static bool LegacyPairIntersectionMathIsPointInTriangle(in Triangle triangle, in RealPoint point)
    {
        var a = new RealVector(triangle.P0.X, triangle.P0.Y, triangle.P0.Z);
        var b = new RealVector(triangle.P1.X, triangle.P1.Y, triangle.P1.Z);
        var c = new RealVector(triangle.P2.X, triangle.P2.Y, triangle.P2.Z);

        var v0 = b - a;
        var v1 = c - a;
        var pointVector = new RealVector(point.X, point.Y, point.Z);
        var v2 = pointVector - a;

        double d00 = v0.Dot(v0);
        double d01 = v0.Dot(v1);
        double d11 = v1.Dot(v1);
        double d20 = v2.Dot(v0);
        double d21 = v2.Dot(v1);

        double denom = d00 * d11 - d01 * d01;
        if (System.Math.Abs(denom) < Tolerances.TrianglePredicateEpsilon)
            return false;

        double invDenom = 1.0 / denom;
        double v = (d11 * d20 - d01 * d21) * invDenom;
        double w = (d00 * d21 - d01 * d20) * invDenom;
        double u = 1.0 - v - w;

        double epsilon = Tolerances.TrianglePredicateEpsilon;
        if (u < -epsilon || v < -epsilon || w < -epsilon)
            return false;

        return true;
    }

    private static bool LegacyPredicateIsPointInTriangle(in Triangle triangle, in RealPoint point)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        var a = new RealVector(triangle.P0.X, triangle.P0.Y, triangle.P0.Z);
        var b = new RealVector(triangle.P1.X, triangle.P1.Y, triangle.P1.Z);
        var c = new RealVector(triangle.P2.X, triangle.P2.Y, triangle.P2.Z);

        var edgeAC = new RealVector(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
        var edgeAB = new RealVector(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var fromAToPoint = new RealVector(point.X - a.X, point.Y - a.Y, point.Z - a.Z);

        double dotEdgeACWithEdgeAC = edgeAC.Dot(edgeAC);
        double dotEdgeACWithEdgeAB = edgeAC.Dot(edgeAB);
        double dotEdgeACWithPoint = edgeAC.Dot(fromAToPoint);
        double dotEdgeABWithEdgeAB = edgeAB.Dot(edgeAB);
        double dotEdgeABWithPoint = edgeAB.Dot(fromAToPoint);

        double denominator =
            dotEdgeACWithEdgeAC * dotEdgeABWithEdgeAB - dotEdgeACWithEdgeAB * dotEdgeACWithEdgeAB;

        if (System.Math.Abs(denominator) < epsilon)
            return false;

        double inverseDenominator = 1.0 / denominator;
        double coordinateU =
            (dotEdgeABWithEdgeAB * dotEdgeACWithPoint - dotEdgeACWithEdgeAB * dotEdgeABWithPoint) *
            inverseDenominator;
        double coordinateV =
            (dotEdgeACWithEdgeAC * dotEdgeABWithPoint - dotEdgeACWithEdgeAB * dotEdgeACWithPoint) *
            inverseDenominator;

        if (coordinateU < -epsilon || coordinateV < -epsilon)
            return false;

        if (coordinateU + coordinateV > 1.0 + epsilon)
            return false;

        return true;
    }

    [Fact]
    public void Triangle_IsPointInsidePredicate_Matches_LegacyImplementations_OnSample()
    {
        var tri = new Triangle(
            new Point(0, 0, 0),
            new Point(4, 0, 0),
            new Point(0, 4, 0),
            new Point(0, 0, 1));

        var samples = new[]
        {
            new RealPoint(0.0, 0.0, 0.0), // P0
            new RealPoint(4.0, 0.0, 0.0), // P1
            new RealPoint(0.0, 4.0, 0.0), // P2
            new RealPoint(2.0, 0.0, 0.0), // edge midpoint P0-P1
            new RealPoint(0.0, 2.0, 0.0), // edge midpoint P0-P2
            new RealPoint(1.0, 1.0, 0.0), // interior
            new RealPoint(-1.0, 0.0, 0.0), // outside
            new RealPoint(5.0, 5.0, 0.0) // outside
        };

        foreach (var p in samples)
        {
            var realTri = new RealTriangle(tri);
            bool newResult = RealTrianglePredicates.IsPointInsidePredicate(in realTri, in p);
            bool legacyPair = LegacyPairIntersectionMathIsPointInTriangle(in tri, in p);
            bool legacyPred = LegacyPredicateIsPointInTriangle(in tri, in p);

            Assert.Equal(legacyPair, newResult);
            Assert.Equal(legacyPred, newResult);
        }
    }
}
