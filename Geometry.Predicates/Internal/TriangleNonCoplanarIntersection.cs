using System;
using System.Collections.Generic;
using Geometry.Predicates;

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
        var realPoints = new List<RealPoint>(uniquePoints.Count);
        for (int i = 0; i < uniquePoints.Count; i++)
        {
            var v = uniquePoints[i];
            realPoints.Add(new RealPoint(v.X, v.Y, v.Z));
        }

        for (int i = 0; i < realPoints.Count - 1; i++)
        {
            var pi = realPoints[i];
            for (int j = i + 1; j < realPoints.Count; j++)
            {
                var pj = realPoints[j];
                double squaredDistance = pi.DistanceSquared(in pj);
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
        var realTargetTriangle = new RealTriangle(targetTriangle);

        // First, handle vertices of the source triangle that lie on the target plane.
        AddVertexIfOnPlaneAndInside(sourceTriangle.P0, targetPlane, in realTargetTriangle, intersectionPoints);
        AddVertexIfOnPlaneAndInside(sourceTriangle.P1, targetPlane, in realTargetTriangle, intersectionPoints);
        AddVertexIfOnPlaneAndInside(sourceTriangle.P2, targetPlane, in realTargetTriangle, intersectionPoints);

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

            var startVector = new RealVector(start.X, start.Y, start.Z);
            var endVector = new RealVector(end.X, end.Y, end.Z);

            var intersectionPoint = new RealVector(
                startVector.X + t * (endVector.X - startVector.X),
                startVector.Y + t * (endVector.Y - startVector.Y),
                startVector.Z + t * (endVector.Z - startVector.Z));

            var intersectionPointReal = new RealPoint(intersectionPoint.X, intersectionPoint.Y, intersectionPoint.Z);
            if (RealTrianglePredicates.IsInsideStrict(realTargetTriangle, intersectionPointReal))
            {
                AddUniqueIntersectionPoint(intersectionPoints, in intersectionPoint);
            }
        }
    }

    private static void AddVertexIfOnPlaneAndInside(
        in Point vertex,
        in Plane targetPlane,
        in RealTriangle targetTriangle,
        List<RealVector> intersectionPoints)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        double distance = targetPlane.SignedDistance(vertex);
        if (Math.Abs(distance) > epsilon)
        {
            return;
        }

        var vertexVector = new RealVector(vertex.X, vertex.Y, vertex.Z);
        var vertexPoint = new RealPoint(vertexVector.X, vertexVector.Y, vertexVector.Z);
        if (RealTrianglePredicates.IsInsideStrict(targetTriangle, vertexPoint))
        {
            AddUniqueIntersectionPoint(intersectionPoints, in vertexVector);
        }
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

