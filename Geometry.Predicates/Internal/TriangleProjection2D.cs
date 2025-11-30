using System;
using System.Collections.Generic;

namespace Geometry.Predicates.Internal;

internal static class TriangleProjection2D
{
    internal enum ProjectionPlane
    {
        YZ, // drop X
        XZ, // drop Y
        XY  // drop Z
    }

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

    internal static ProjectionPlane ChooseProjectionAxis(in RealNormal normal)
    {
        var ax = Math.Abs(normal.X);
        var ay = Math.Abs(normal.Y);
        var az = Math.Abs(normal.Z);

        if (ax >= ay && ax >= az) return ProjectionPlane.YZ; // drop X, keep (Y,Z)
        if (ay >= ax && ay >= az) return ProjectionPlane.XZ; // drop Y, keep (X,Z)
        return ProjectionPlane.XY;                           // drop Z, keep (X,Y)
    }

    internal static Point2D ProjectPointTo2D(in Point p, ProjectionPlane plane) =>
        plane switch
        {
            ProjectionPlane.YZ => new Point2D(p.Y, p.Z),
            ProjectionPlane.XZ => new Point2D(p.X, p.Z),
            _ => new Point2D(p.X, p.Y),
        };

    internal static void ProjectTriangleTo2D(
        in Triangle triangle,
        ProjectionPlane plane,
        out Point2D t0,
        out Point2D t1,
        out Point2D t2)
    {
        t0 = ProjectPointTo2D(in triangle.P0, plane);
        t1 = ProjectPointTo2D(in triangle.P1, plane);
        t2 = ProjectPointTo2D(in triangle.P2, plane);

        // Sanity check: projected triangle must have non-zero area in 2D.
        var v0 = new Point2D(t1.X - t0.X, t1.Y - t0.Y);
        var v1 = new Point2D(t2.X - t0.X, t2.Y - t0.Y);
        double epsilon = Tolerances.TrianglePredicateEpsilon;
        if (Math.Abs(Cross(in v0, in v1)) <= epsilon)
        {
            throw new System.Exception();
        }
    }

    internal static void AddIfInsideTriangle(
        in Point2D candidate,
        in Point2D t0,
        in Point2D t1,
        in Point2D t2,
        List<Point2D> output)
    {
        if (IsPointInTriangle(candidate, t0, t1, t2))
        {
            AddUnique(output, candidate);
        }
    }

    internal static bool IsPointInTriangle(in Point2D p, in Point2D t0, in Point2D t1, in Point2D t2)
    {
        // Barycentric test with edge-inclusive containment.
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

        double denominator = dY12 * dX02 + dX21 * dY02;
        double s = dY12 * dX + dX21 * dY;
        double t = (y2 - y0) * dX + (x0 - x2) * dY;

        if (denominator < 0)
        {
            denominator = -denominator;
            s = -s;
            t = -t;
        }

        return s >= 0 && t >= 0 && (s + t) <= denominator;
    }

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

        double denominator = dY12 * dX02 + dX21 * dY02;
        if (denominator == 0.0)
        {
            // Degenerate projected triangle; should not occur for well-formed input.
            throw new System.Exception();
        }

        double s = dY12 * dX + dX21 * dY;
        double t = (y2 - y0) * dX + (x0 - x2) * dY;

        double u = s / denominator;
        double v = t / denominator;
        double w = 1.0 - u - v;

        return new Barycentric(u, v, w);
    }

    internal static bool TrySegmentIntersection(
        in Point2D p0,
        in Point2D p1,
        in Point2D q0,
        in Point2D q1,
        out Point2D intersection)
    {
        var pDirection = new Point2D(p1.X - p0.X, p1.Y - p0.Y);
        var qDirection = new Point2D(q1.X - q0.X, q1.Y - q0.Y);

        double denominator = Cross(pDirection, qDirection);
        double epsilon = Tolerances.TrianglePredicateEpsilon;

        if (Math.Abs(denominator) < epsilon)
        {
            intersection = default;
            return false; // Parallel or collinear; no unique intersection point to add.
        }

        var qMinusP = new Point2D(q0.X - p0.X, q0.Y - p0.Y);
        double t = Cross(qMinusP, qDirection) / denominator;
        double u = Cross(qMinusP, pDirection) / denominator;

        if (t < -epsilon || t > 1.0 + epsilon || u < -epsilon || u > 1.0 + epsilon)
        {
            intersection = default;
            return false; // Intersection lies outside one of the segments.
        }

        if (t < 0) t = 0;
        else if (t > 1.0) t = 1.0;

        intersection = new Point2D(p0.X + t * pDirection.X, p0.Y + t * pDirection.Y);
        return true;
    }

    internal static double Cross(in Point2D a, in Point2D b)
        => a.X * b.Y - a.Y * b.X;

    internal static bool HasNonCollinearTriple(List<Point2D> points)
    {
        if (points.Count < 3) return false;

        double epsilon = Tolerances.TrianglePredicateEpsilon;
        for (int i = 0; i < points.Count - 2; i++)
        {
            var pi = points[i];
            for (int j = i + 1; j < points.Count - 1; j++)
            {
                var pj = points[j];
                var v1 = new Point2D(pj.X - pi.X, pj.Y - pi.Y);
                for (int k = j + 1; k < points.Count; k++)
                {
                    var pk = points[k];
                    var v2 = new Point2D(pk.X - pi.X, pk.Y - pi.Y);
                    if (Math.Abs(Cross(v1, v2)) > epsilon)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    internal static void AddUnique(List<Point2D> points, in Point2D candidate)
    {
        double epsilon = Tolerances.TrianglePredicateEpsilon;
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (Math.Abs(p.X - candidate.X) <= epsilon &&
                Math.Abs(p.Y - candidate.Y) <= epsilon)
            {
                return;
            }
        }
        points.Add(candidate);
    }
}
