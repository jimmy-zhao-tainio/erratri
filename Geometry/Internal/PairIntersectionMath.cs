using System;
using System.Collections.Generic;

namespace Geometry.Internal;

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
    internal readonly struct Point2D
    {
        public readonly double X;
        public readonly double Y;

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

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
        in Triangle triA,
        in Triangle triB)
    {
        var planeA = Plane.FromTriangle(in triA);
        var planeB = Plane.FromTriangle(in triB);

        if (!IntersectsPlane(in triA, in planeB) ||
            !IntersectsPlane(in triB, in planeA))
        {
            return new List<RealVector>();
        }

        var rawPoints = new List<RealVector>(4);

        CollectTrianglePlaneIntersections(in triA, in planeB, in triB, rawPoints);
        CollectTrianglePlaneIntersections(in triB, in planeA, in triA, rawPoints);

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
    internal static List<Point2D> ComputeCoplanarIntersectionPoints(
        in Triangle triA,
        in Triangle triB,
        out int projectionAxis)
    {
        projectionAxis = ChooseProjectionAxis(triA.Normal);

        var a0 = ProjectTo2D(triA.P0, projectionAxis);
        var a1 = ProjectTo2D(triA.P1, projectionAxis);
        var a2 = ProjectTo2D(triA.P2, projectionAxis);

        var b0 = ProjectTo2D(triB.P0, projectionAxis);
        var b1 = ProjectTo2D(triB.P1, projectionAxis);
        var b2 = ProjectTo2D(triB.P2, projectionAxis);

        var candidates = new List<Point2D>(12);

        // Vertices of A inside B (including on boundary)
        AddIfInsideTriangle(a0, b0, b1, b2, candidates);
        AddIfInsideTriangle(a1, b0, b1, b2, candidates);
        AddIfInsideTriangle(a2, b0, b1, b2, candidates);

        // Vertices of B inside A
        AddIfInsideTriangle(b0, a0, a1, a2, candidates);
        AddIfInsideTriangle(b1, a0, a1, a2, candidates);
        AddIfInsideTriangle(b2, a0, a1, a2, candidates);

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
                if (TrySegmentIntersection(aStart, aEnd, bStart, bEnd, out var intersection))
                {
                    AddUniquePoint(candidates, intersection);
                }
            }
        }

        return candidates;
    }

    // 3D barycentric point-in-triangle test aligned with
    // Geometry.Predicates.Internal.TriangleNonCoplanarIntersection.
    // Edge vectors and dot products follow the same pattern so
    // feature construction does not diverge from the predicate layer.
    internal static bool IsPointInTriangle(
        in RealVector point,
        in Triangle triangle)
    {
        var a = ToVector(triangle.P0);
        var b = ToVector(triangle.P1);
        var c = ToVector(triangle.P2);

        var v0 = b - a;
        var v1 = c - a;
        var v2 = point - a;

        double d00 = v0.Dot(v0);
        double d01 = v0.Dot(v1);
        double d11 = v1.Dot(v1);
        double d20 = v2.Dot(v0);
        double d21 = v2.Dot(v1);

        double denom = d00 * d11 - d01 * d01;
        if (Math.Abs(denom) < Tolerances.TrianglePredicateEpsilon)
        {
            return false;
        }

        double invDenom = 1.0 / denom;
        double v = (d11 * d20 - d01 * d21) * invDenom;
        double w = (d00 * d21 - d01 * d20) * invDenom;
        double u = 1.0 - v - w;

        double epsilon = Tolerances.TrianglePredicateEpsilon;
        if (u < -epsilon || v < -epsilon || w < -epsilon)
        {
            return false;
        }

        return true;
    }

    internal static void ProjectTriangleTo2D(
        in Triangle tri,
        int axis,
        out Point2D t0,
        out Point2D t1,
        out Point2D t2)
    {
        t0 = ProjectTo2D(tri.P0, axis);
        t1 = ProjectTo2D(tri.P1, axis);
        t2 = ProjectTo2D(tri.P2, axis);
    }

    // Convert a 3D point into barycentric coordinates (u, v, w) for a given
    // triangle in 3D.
    //
    // In plain words: find the numbers u, v, w such that
    //
    //   p ≈ u * t0 + v * t1 + w * t2,  with  u + v + w = 1
    //
    // This lets us store "the same point" in a triangle-local way instead
    // of as a raw Vector.
    internal static Barycentric ToBarycentric(
        in Triangle tri,
        in RealVector point)
    {
        var p0 = new RealVector(tri.P0.X, tri.P0.Y, tri.P0.Z);
        var p1 = new RealVector(tri.P1.X, tri.P1.Y, tri.P1.Z);
        var p2 = new RealVector(tri.P2.X, tri.P2.Y, tri.P2.Z);

        var v0 = p1 - p0;
        var v1 = p2 - p0;
        var v2 = point - p0;

        var d00 = v0.Dot(v0);
        var d01 = v0.Dot(v1);
        var d11 = v1.Dot(v1);
        var d20 = v2.Dot(v0);
        var d21 = v2.Dot(v1);

        var denom = d00 * d11 - d01 * d01;
        if (denom == 0.0)
        {
            // Degenerate triangle metric; should not happen for valid input.
            // Callers should treat this as an error path and expect the
            // resulting barycentric to be aggressively merged.
            System.Diagnostics.Debug.Assert(false, "Degenerate triangle in ToBarycentric.");
            return new Barycentric(0.0, 0.0, 0.0);
        }

        var invDenom = 1.0 / denom;
        var v = (d11 * d20 - d01 * d21) * invDenom;
        var w = (d00 * d21 - d01 * d20) * invDenom;
        var u = 1.0 - v - w;
        return new Barycentric(u, v, w);
    }

    // Same as ToBarycentric, but in the 2D projected plane.
    //
    // Used in the coplanar case, where both triangles live in the same plane
    // and we have already projected them down to 2D (Point2D).
    //
    // The output is still Barycentric so the rest of the code can treat
    // 2D and 3D barycentric data the same way.
    internal static Barycentric ToBarycentric2D(
        in Point2D p,
        in Point2D t0,
        in Point2D t1,
        in Point2D t2)
    {
        double x = p.X, y = p.Y;

        double x0 = t0.X, y0 = t0.Y;
        double x1 = t1.X, y1 = t1.Y;
        double x2 = t2.X, y2 = t2.Y;

        double dX = x - x2;
        double dY = y - y2;
        double dX21 = x2 - x1;
        double dY12 = y1 - y2;
        double dX02 = x0 - x2;
        double dY02 = y0 - y2;

        double denom = dY12 * dX02 + dX21 * dY02;
        if (denom == 0.0)
        {
            // Degenerate projected triangle; mirrors the 3D barycentric
            // fallback and should not occur for well-formed input.
            System.Diagnostics.Debug.Assert(false, "Degenerate triangle in ToBarycentric2D.");
            return new Barycentric(0.0, 0.0, 0.0);
        }

        double s = dY12 * dX + dX21 * dY;
        double t = (y2 - y0) * dX + (x0 - x2) * dY;

        double u = s / denom;
        double v = t / denom;
        double w = 1.0 - u - v;

        return new Barycentric(u, v, w);
    }

    private static bool IntersectsPlane(
        in Triangle triangle,
        in Plane plane)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        double d0 = plane.Evaluate(triangle.P0);
        double d1 = plane.Evaluate(triangle.P1);
        double d2 = plane.Evaluate(triangle.P2);

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

            double distanceStart = targetPlane.Evaluate(start);
            double distanceEnd = targetPlane.Evaluate(end);

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

            if (IsPointInTriangle(intersectionPoint, in targetTriangle))
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
        double distance = targetPlane.Evaluate(vertex);
        if (Math.Abs(distance) > Tolerances.TrianglePredicateEpsilon)
        {
            return;
        }

        var vertexVector = ToVector(vertex);
        if (IsPointInTriangle(vertexVector, in targetTriangle))
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

    private static int ChooseProjectionAxis(in RealNormal normal)
    {
        var ax = Math.Abs(normal.X);
        var ay = Math.Abs(normal.Y);
        var az = Math.Abs(normal.Z);

        if (ax >= ay && ax >= az) return 0;
        if (ay >= ax && ay >= az) return 1;
        return 2;
    }

    private static Point2D ProjectTo2D(in Point p, int axis) =>
        axis switch
        {
            0 => new Point2D(p.Y, p.Z),
            1 => new Point2D(p.X, p.Z),
            _ => new Point2D(p.X, p.Y),
        };

    // 2D point-in-triangle test mirroring the logic in
    // Geometry.Predicates.Internal.TriangleProjection2D. Uses the
    // same orientation and edge-inclusive containment conventions.
    private static bool IsPointInTriangle2D(
        in Point2D p,
        in Point2D t0,
        in Point2D t1,
        in Point2D t2)
    {
        double x = p.X, y = p.Y;

        double x0 = t0.X, y0 = t0.Y;
        double x1 = t1.X, y1 = t1.Y;
        double x2 = t2.X, y2 = t2.Y;

        double dX = x - x2;
        double dY = y - y2;
        double dX21 = x2 - x1;
        double dY12 = y1 - y2;
        double dX02 = x0 - x2;
        double dY02 = y0 - y2;

        double denom = dY12 * dX02 + dX21 * dY02;
        double s = dY12 * dX + dX21 * dY;
        double t = (y2 - y0) * dX + (x0 - x2) * dY;

        if (denom < 0)
        {
            denom = -denom;
            s = -s;
            t = -t;
        }

        return s >= 0 && t >= 0 && (s + t) <= denom;
    }

    private static void AddIfInsideTriangle(
        in Point2D candidate,
        in Point2D t0,
        in Point2D t1,
        in Point2D t2,
        List<Point2D> output)
    {
        if (IsPointInTriangle2D(candidate, t0, t1, t2))
        {
            AddUniquePoint(output, candidate);
        }
    }

    // 2D segment/segment intersection closely follows the structure
    // of TriangleProjection2D.TrySegmentIntersection: same parameter
    // representation and epsilon semantics.
    private static bool TrySegmentIntersection(
        in Point2D p0,
        in Point2D p1,
        in Point2D q0,
        in Point2D q1,
        out Point2D intersection)
    {
        var pDirection = new Point2D(p1.X - p0.X, p1.Y - p0.Y);
        var qDirection = new Point2D(q1.X - q0.X, q1.Y - q0.Y);

        double denominator = Cross(pDirection, qDirection);

        if (Math.Abs(denominator) < Tolerances.TrianglePredicateEpsilon)
        {
            intersection = default;
            return false;
        }

        var qMinusP = new Point2D(q0.X - p0.X, q0.Y - p0.Y);
        double t = Cross(qMinusP, qDirection) / denominator;
        double u = Cross(qMinusP, pDirection) / denominator;

        double epsilon = Tolerances.TrianglePredicateEpsilon;
        if (t < -epsilon || t > 1.0 + epsilon || u < -epsilon || u > 1.0 + epsilon)
        {
            intersection = default;
            return false;
        }

        if (t < 0) t = 0;
        else if (t > 1.0) t = 1.0;

        intersection = new Point2D(p0.X + t * pDirection.X, p0.Y + t * pDirection.Y);
        return true;
    }

    private static double Cross(in Point2D a, in Point2D b)
        => a.X * b.Y - a.Y * b.X;

    private static void AddUniquePoint(
        List<Point2D> points,
        in Point2D candidate)
    {
        double squaredEpsilon = Tolerances.FeatureWorldDistanceEpsilonSquared;
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            double dx = p.X - candidate.X;
            double dy = p.Y - candidate.Y;
            double squaredDistance = dx * dx + dy * dy;
            if (squaredDistance <= squaredEpsilon)
            {
                return;
            }
        }

        points.Add(candidate);
    }
}
