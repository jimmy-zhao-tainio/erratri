using System;
using System.Collections.Generic;

namespace Geometry.Predicates.Internal;

internal static class TriangleNonCoplanarIntersection
{
    internal static TriangleIntersection Classify(in Triangle first, in Triangle second)
    {
        // Non-coplanar triangles can intersect only in a point or a segment.
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        var planeFirst = Plane.FromTriangle(first);
        var planeSecond = Plane.FromTriangle(second);

        // Quick rejection: if either triangle lies strictly on one side of
        // the other's plane (by more than epsilon), there is no intersection.
        if (!IntersectsPlane(first, planeSecond) ||
            !IntersectsPlane(second, planeFirst))
        {
            return new TriangleIntersection(TriangleIntersectionType.None);
        }

        var intersectionPoints = new List<RealVector>(4);

        CollectTrianglePlaneIntersections(first, planeSecond, in second, intersectionPoints);
        CollectTrianglePlaneIntersections(second, planeFirst, in first, intersectionPoints);

        if (intersectionPoints.Count == 0)
        {
            return new TriangleIntersection(TriangleIntersectionType.None);
        }

        // Deduplicate with a distance-based filter.
        var uniquePoints = new List<RealVector>(intersectionPoints.Count);
        foreach (var point in intersectionPoints)
        {
            AddUniqueIntersectionPoint(uniquePoints, in point);
        }

        if (uniquePoints.Count == 0)
        {
            return new TriangleIntersection(TriangleIntersectionType.None);
        }

        if (uniquePoints.Count == 1)
        {
            // Exactly one intersection point.
            return new TriangleIntersection(TriangleIntersectionType.Point);
        }

        // More than one distinct point: check whether we have a genuine segment.
        double maximumSquaredDistance = 0.0;
        for (int i = 0; i < uniquePoints.Count - 1; i++)
        {
            var pi = uniquePoints[i];
            for (int j = i + 1; j < uniquePoints.Count; j++)
            {
                var pj = uniquePoints[j];
                double dx = pj.X - pi.X;
                double dy = pj.Y - pi.Y;
                double dz = pj.Z - pi.Z;
                double squaredDistance = dx * dx + dy * dy + dz * dz;
                if (squaredDistance > maximumSquaredDistance)
                {
                    maximumSquaredDistance = squaredDistance;
                }
            }
        }

        double squaredEpsilon = epsilon * epsilon;
        if (maximumSquaredDistance <= squaredEpsilon)
        {
            // All intersection samples collapse to a single point within tolerance.
            return new TriangleIntersection(TriangleIntersectionType.Point);
        }

        // Genuine segment intersection.
        return new TriangleIntersection(TriangleIntersectionType.Segment);
    }

    private static bool IntersectsPlane(in Triangle triangle, in Plane plane)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        double distance0 = plane.SignedDistance(triangle.P0);
        double distance1 = plane.SignedDistance(triangle.P1);
        double distance2 = plane.SignedDistance(triangle.P2);

        bool allPositive = distance0 > epsilon && distance1 > epsilon && distance2 > epsilon;
        bool allNegative = distance0 < -epsilon && distance1 < -epsilon && distance2 < -epsilon;

        return !(allPositive || allNegative);
    }

    private static void CollectTrianglePlaneIntersections(
        in Triangle sourceTriangle,
        in Plane targetPlane,
        in Triangle targetTriangle,
        List<RealVector> intersectionPoints)
    {
        // First, handle vertices of the source triangle that lie on the target plane.
        AddVertexIfOnPlaneAndInside(sourceTriangle.P0, targetPlane, in targetTriangle, intersectionPoints);
        AddVertexIfOnPlaneAndInside(sourceTriangle.P1, targetPlane, in targetTriangle, intersectionPoints);
        AddVertexIfOnPlaneAndInside(sourceTriangle.P2, targetPlane, in targetTriangle, intersectionPoints);

        var vertices = new[] { sourceTriangle.P0, sourceTriangle.P1, sourceTriangle.P2 };

        for (int i = 0; i < 3; i++)
        {
            var start = vertices[i];
            var end = vertices[(i + 1) % 3];

            double distanceStart = targetPlane.SignedDistance(start);
            double distanceEnd = targetPlane.SignedDistance(end);

            // If both endpoints are on the same side of the plane and not within
            // epsilon of it, the segment does not cross the plane.
            double epsilon = Tolerances.TrianglePredicateEpsilon;
            if (distanceStart > epsilon && distanceEnd > epsilon) continue;
            if (distanceStart < -epsilon && distanceEnd < -epsilon) continue;

            bool hasOppositeSigns =
                (distanceStart > epsilon && distanceEnd < -epsilon) ||
                (distanceStart < -epsilon && distanceEnd > epsilon);

            if (!hasOppositeSigns)
            {
                continue;
            }

            double t = distanceStart / (distanceStart - distanceEnd);

            var startVector = ToVector(start);
            var endVector = ToVector(end);

            var intersectionPoint = new RealVector(
                startVector.X + t * (endVector.X - startVector.X),
                startVector.Y + t * (endVector.Y - startVector.Y),
                startVector.Z + t * (endVector.Z - startVector.Z));

            var intersectionPointReal = new RealPoint(intersectionPoint.X, intersectionPoint.Y, intersectionPoint.Z);
            if (IsPointInTriangle(in intersectionPointReal, in targetTriangle))
            {
                AddUniqueIntersectionPoint(intersectionPoints, in intersectionPoint);
            }
        }
    }

    private static void AddVertexIfOnPlaneAndInside(
        in Point vertex,
        in Plane targetPlane,
        in Triangle targetTriangle,
        List<RealVector> intersectionPoints)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        double distance = targetPlane.SignedDistance(vertex);
        if (Math.Abs(distance) > epsilon)
        {
            return;
        }

        var vertexVector = ToVector(vertex);
        var vertexPoint = new RealPoint(vertexVector.X, vertexVector.Y, vertexVector.Z);
        if (IsPointInTriangle(in vertexPoint, in targetTriangle))
        {
            AddUniqueIntersectionPoint(intersectionPoints, in vertexVector);
        }
    }

    private static RealVector ToVector(in Point point)
        => new RealVector(point.X, point.Y, point.Z);

    private static bool IsPointInTriangle(
        in RealPoint point,
        in Triangle triangle)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        var a = ToVector(triangle.P0);
        var b = ToVector(triangle.P1);
        var c = ToVector(triangle.P2);

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

        if (Math.Abs(denominator) < epsilon)
        {
            // Degenerate triangle in the chosen metric; should not happen for well-formed input.
            return false;
        }

        double inverseDenominator = 1.0 / denominator;
        double coordinateU =
            (dotEdgeABWithEdgeAB * dotEdgeACWithPoint - dotEdgeACWithEdgeAB * dotEdgeABWithPoint) *
            inverseDenominator;
        double coordinateV =
            (dotEdgeACWithEdgeAC * dotEdgeABWithPoint - dotEdgeACWithEdgeAB * dotEdgeACWithPoint) *
            inverseDenominator;

        if (coordinateU < -epsilon || coordinateV < -epsilon)
        {
            return false;
        }

        if (coordinateU + coordinateV > 1.0 + epsilon)
        {
            return false;
        }

        return true;
    }

    private static void AddUniqueIntersectionPoint(
        List<RealVector> points,
        in RealVector candidate)
    {
        double squaredEpsilon = Tolerances.FeatureWorldDistanceEpsilonSquared;
        for (int i = 0; i < points.Count; i++)
        {
            var existing = points[i];
            var existingPoint = new RealPoint(existing.X, existing.Y, existing.Z);
            var candidatePoint = new RealPoint(candidate.X, candidate.Y, candidate.Z);
            double squaredDistance = existingPoint.DistanceSquared(in candidatePoint);
            if (squaredDistance <= squaredEpsilon)
            {
                return;
            }
        }

        points.Add(candidate);
    }
}

