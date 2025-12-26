using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates;

namespace Geometry.Predicates.Internal;

// Helper functions for *one* triangle pair.
//
// This file is the "raw geometry" layer. Given two triangles it can
// tell you:
//
//   - Which points lie on both triangles (non-coplanar case)
//   - How the intersection polygon looks in 2D (coplanar case)
//   - Barycentric coordinates for those points
//
// It does NOT know anything about meshes, PairFeatures, or graphs.
// Kernel code calls these functions, then wraps the results up in
// PairVertex / PairSegment.
internal static class PairIntersectionMath
{
    // NON-COPLANAR case:
    //
    // This collects all world-space points where triangleA and triangleB
    // touch when they are NOT coplanar.
    //
    // The list can contain:
    //   - 0 points  => no intersection
    //   - 1 point   => touch at a vertex
    //   - 2 points  => segment
    //   - 3+ points => degenerate / corner cases
    //
    // Deduplication is done with a small epsilon so we don't get the same
    // point twice from different edge/vertex combinations.
    internal static List<RealVector> ComputeNonCoplanarIntersectionPoints(
        in Triangle triangleA,
        in Triangle triangleB)
    {
        var planeA = Plane.FromTriangle(in triangleA);
        var planeB = Plane.FromTriangle(in triangleB);

        if (!IntersectsPlane(in triangleA, in planeB) ||
            !IntersectsPlane(in triangleB, in planeA))
        {
            return new List<RealVector>();
        }

        var rawPoints = new List<RealVector>(4);

        CollectTrianglePlaneIntersections(in triangleA, in planeB, in triangleB, rawPoints);
        CollectTrianglePlaneIntersections(in triangleB, in planeA, in triangleA, rawPoints);

        if (rawPoints.Count == 0)
        {
            return rawPoints;
        }

        // Deduplicate with a distance-based filter in world space,
        // mirroring TriangleNonCoplanarIntersection.AddUniqueIntersectionPoint.
        var unique = new List<RealVector>(rawPoints.Count);
        foreach (var p in rawPoints)
        {
            AddUniqueIntersectionPoint(unique, in p);
        }

        return unique;
    }

    // COPLANAR case:
    //
    // Here both triangles lie in the same plane. We project them to 2D,
    // compute the overlap polygon between the two triangles, and return
    // its vertices in that 2D space.
    //
    // Later we map these 2D points back to barycentric coords on each
    // original triangle.
    internal static List<RealPoint2D> ComputeCoplanarIntersectionPoints(
        in Triangle triangleA,
        in Triangle triangleB)
    {
        var projectionPlane = TriangleProjection2D.ChooseProjectionAxis(triangleA.Normal);

        var a0 = TriangleProjection2D.ProjectPointTo2D(triangleA.P0, projectionPlane);
        var a1 = TriangleProjection2D.ProjectPointTo2D(triangleA.P1, projectionPlane);
        var a2 = TriangleProjection2D.ProjectPointTo2D(triangleA.P2, projectionPlane);

        var b0 = TriangleProjection2D.ProjectPointTo2D(triangleB.P0, projectionPlane);
        var b1 = TriangleProjection2D.ProjectPointTo2D(triangleB.P1, projectionPlane);
        var b2 = TriangleProjection2D.ProjectPointTo2D(triangleB.P2, projectionPlane);

        var candidates2D = new List<TriangleProjection2D.Point2D>(12);

        // Vertices of A inside B (including on boundary)
        TriangleProjection2D.AddIfInsideTriangle(a0, b0, b1, b2, candidates2D);
        TriangleProjection2D.AddIfInsideTriangle(a1, b0, b1, b2, candidates2D);
        TriangleProjection2D.AddIfInsideTriangle(a2, b0, b1, b2, candidates2D);

        // Vertices of B inside A
        TriangleProjection2D.AddIfInsideTriangle(b0, a0, a1, a2, candidates2D);
        TriangleProjection2D.AddIfInsideTriangle(b1, a0, a1, a2, candidates2D);
        TriangleProjection2D.AddIfInsideTriangle(b2, a0, a1, a2, candidates2D);

        // Edge-edge intersections
        var aVerts = new[] { a0, a1, a2 };
        var bVerts = new[] { b0, b1, b2 };
        for (int i = 0; i < 3; i++)
        {
            var aStart = aVerts[i];
            var aEnd = aVerts[(i + 1) % 3];
            for (int j = 0; j < 3; j++)
            {
                var bStart = bVerts[j];
                var bEnd = bVerts[(j + 1) % 3];
                if (TriangleProjection2D.TrySegmentIntersection(aStart, aEnd, bStart, bEnd, out var intersection))
                {
                    TriangleProjection2D.AddUnique(candidates2D, intersection);
                }
            }
        }

        var result = new List<RealPoint2D>(candidates2D.Count);
        for (int i = 0; i < candidates2D.Count; i++)
        {
            var p = candidates2D[i];
            result.Add(new RealPoint2D(p.X, p.Y));
        }

        return result;
    }


    private static bool IntersectsPlane(
        in Triangle triangle,
        in Plane plane)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        double d0 = plane.SignedDistance(triangle.P0);
        double d1 = plane.SignedDistance(triangle.P1);
        double d2 = plane.SignedDistance(triangle.P2);

        bool allPositive = d0 > epsilon && d1 > epsilon && d2 > epsilon;
        bool allNegative = d0 < -epsilon && d1 < -epsilon && d2 < -epsilon;

        return !(allPositive || allNegative);
    }

    private static void CollectTrianglePlaneIntersections(
        in Triangle sourceTriangle,
        in Plane targetPlane,
        in Triangle targetTriangle,
        List<RealVector> intersectionPoints)
    {
        AddVertexIfOnPlaneAndInside(sourceTriangle.P0, in targetPlane, in targetTriangle, intersectionPoints);
        AddVertexIfOnPlaneAndInside(sourceTriangle.P1, in targetPlane, in targetTriangle, intersectionPoints);
        AddVertexIfOnPlaneAndInside(sourceTriangle.P2, in targetPlane, in targetTriangle, intersectionPoints);

        var vertices = new[] { sourceTriangle.P0, sourceTriangle.P1, sourceTriangle.P2 };

        for (int i = 0; i < 3; i++)
        {
            var start = vertices[i];
            var end = vertices[(i + 1) % 3];

            double distanceStart = targetPlane.SignedDistance(start);
            double distanceEnd = targetPlane.SignedDistance(end);

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

            var realTriangle = new RealTriangle(targetTriangle);
            var intersectionPointReal = new RealPoint(intersectionPoint.X, intersectionPoint.Y, intersectionPoint.Z);
            if (RealTrianglePredicates.IsInsideStrict(realTriangle, intersectionPointReal))
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
        double distance = targetPlane.SignedDistance(vertex);
        if (Math.Abs(distance) > Tolerances.TrianglePredicateEpsilon)
        {
            return;
        }

        var vertexVector = ToVector(vertex);
        var realTriangle = new RealTriangle(targetTriangle);
        var vertexPoint = new RealPoint(vertexVector.X, vertexVector.Y, vertexVector.Z);
        if (RealTrianglePredicates.IsInsideStrict(realTriangle, vertexPoint))
        {
            AddUniqueIntersectionPoint(intersectionPoints, in vertexVector);
        }
    }

    private static RealVector ToVector(in Point point)
        => new RealVector(point.X, point.Y, point.Z);

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
