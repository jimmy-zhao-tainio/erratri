using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates;
using Kernel;
using KTS = Kernel.TriangleSubdivision;

namespace TriangleSubdivision.Fuzz;

public static class TriangleSubdivisionFuzz
{
    public static void Run(
        int iterations = 100_000,
        int maxPointsPerTriangle = 8,
        int seed = 12345)
    {
        var rng = new Random(seed);

        for (int iter = 0; iter < iterations; iter++)
        {
            var triangle = RandomTriangle(rng);

            double originalArea = Math.Abs(
                new RealTriangle(
                    new RealPoint(triangle.P0),
                    new RealPoint(triangle.P1),
                    new RealPoint(triangle.P2)
                ).SignedArea);

            if (originalArea <= Tolerances.EpsArea)
            {
                iter--;
                continue;
            }

            var points = RandomIntersectionPoints(rng, triangle, maxPointsPerTriangle);
            var segments = RandomNonCrossingSegments(rng, points);

            if (segments.Count == 0 && rng.NextDouble() < 0.7)
            {
                iter--;
                continue;
            }

            IReadOnlyList<RealTriangle> patches;
            try
            {
                patches = KTS.Subdivide(triangle, points, segments);
            }
            catch (Exception ex)
            {
                DumpConfiguration(
                    "Subdivide threw an exception",
                    iter,
                    seed,
                    triangle,
                    points,
                    segments,
                    ex);
                throw;
            }

            double patchAreaSum = 0.0;
            foreach (var rt in patches)
            {
                double a = Math.Abs(rt.SignedArea);
                patchAreaSum += a;
                if (a <= 0.0)
                {
                    DumpConfiguration(
                        "Patch with non-positive area",
                        iter,
                        seed,
                        triangle,
                        points,
                        segments,
                        null);
                    throw new InvalidOperationException("Patch triangle has non-positive area.");
                }
            }

            double diff = Math.Abs(patchAreaSum - originalArea);
            double relTol = Tolerances.BarycentricInsideEpsilon * originalArea;
            if (diff > Tolerances.EpsArea && diff > relTol)
            {
                DumpConfiguration(
                    $"Area mismatch: original={originalArea}, patches={patchAreaSum}, diff={diff}",
                    iter,
                    seed,
                    triangle,
                    points,
                    segments,
                    null);
                throw new InvalidOperationException("Area conservation failed in fuzz test.");
            }
        }
    }

    private static Triangle RandomTriangle(Random rng)
    {
        while (true)
        {
            var p0 = RandomPoint(rng);
            var p1 = RandomPoint(rng);
            var p2 = RandomPoint(rng);

            var tri = new Triangle(p0, p1, p2, missing: p0 + (0, 0, 1));

            double area = Math.Abs(
                new RealTriangle(
                    new RealPoint(tri.P0),
                    new RealPoint(tri.P1),
                    new RealPoint(tri.P2)
                ).SignedArea);

            if (area > Tolerances.EpsArea)
            {
                return tri;
            }
        }
    }

    private static Point RandomPoint(Random rng)
    {
        // Box [-100,100] x [-100,100] x [0,100].
        long x = rng.Next(-100, 101);
        long y = rng.Next(-100, 101);
        long z = rng.Next(0, 101);
        return new Point(x, y, z);
    }

    private static List<KTS.IntersectionPoint> RandomIntersectionPoints(
        Random rng,
        Triangle triangle,
        int maxPoints)
    {
        int n = rng.Next(0, maxPoints + 1);
        var points = new List<KTS.IntersectionPoint>(n);

        for (int i = 0; i < n; i++)
        {
            double r0 = rng.NextDouble();
            double r1 = rng.NextDouble();
            double r2 = rng.NextDouble();
            double inv = 1.0 / (r0 + r1 + r2);
            double u = r0 * inv;
            double v = r1 * inv;
            double w = r2 * inv;
            var bary = new Barycentric(u, v, w);

            var pos = triangle.FromBarycentric(in bary);
            points.Add(new KTS.IntersectionPoint(bary, pos));
        }

        return points;
    }

    private static List<KTS.IntersectionSegment> RandomNonCrossingSegments(
        Random rng,
        IReadOnlyList<KTS.IntersectionPoint> points)
    {
        var segments = new List<KTS.IntersectionSegment>();
        int n = points.Count;
        if (n < 2)
        {
            return segments;
        }

        int targetSegments = rng.Next(1, n * 2 + 1);

        for (int attempt = 0; attempt < targetSegments * 4; attempt++)
        {
            int i = rng.Next(0, n);
            int j = rng.Next(0, n);
            if (i == j) continue;
            if (i > j) (i, j) = (j, i);

            if (HasSegment(segments, i, j)) continue;

            if (WouldCross(points, segments, i, j)) continue;

            segments.Add(new KTS.IntersectionSegment(i, j));
            if (segments.Count >= targetSegments)
            {
                break;
            }
        }

        return segments;
    }

    private static bool HasSegment(
        List<KTS.IntersectionSegment> segments,
        int i,
        int j)
    {
        foreach (var s in segments)
        {
            int a = Math.Min(s.StartIndex, s.EndIndex);
            int b = Math.Max(s.StartIndex, s.EndIndex);
            if (a == i && b == j) return true;
        }

        return false;
    }

    private static bool WouldCross(
        IReadOnlyList<KTS.IntersectionPoint> points,
        List<KTS.IntersectionSegment> segments,
        int i,
        int j)
    {
        var pi = points[i].Barycentric;
        var pj = points[j].Barycentric;
        var segCandidate = new RealSegment(
            new RealPoint(pi.U, pi.V, 0.0),
            new RealPoint(pj.U, pj.V, 0.0));

        foreach (var s in segments)
        {
            if (s.StartIndex == i || s.EndIndex == i ||
                s.StartIndex == j || s.EndIndex == j)
            {
                continue;
            }

            var pa = points[s.StartIndex].Barycentric;
            var pb = points[s.EndIndex].Barycentric;
            var existing = new RealSegment(
                new RealPoint(pa.U, pa.V, 0.0),
                new RealPoint(pb.U, pb.V, 0.0));

            if (RealSegmentPredicates.TryIntersect(segCandidate, existing, out var _))
            {
                return true;
            }
        }

        return false;
    }

    private static void DumpConfiguration(
        string message,
        int iteration,
        int seed,
        Triangle triangle,
        IReadOnlyList<KTS.IntersectionPoint> points,
        IReadOnlyList<KTS.IntersectionSegment> segments,
        Exception? ex)
    {
        Console.WriteLine("---- Fuzz failure ----");
        Console.WriteLine(message);
        Console.WriteLine($"iteration={iteration} seed={seed}");
        Console.WriteLine($"triangle: P0=({triangle.P0.X},{triangle.P0.Y},{triangle.P0.Z}) " +
                          $"P1=({triangle.P1.X},{triangle.P1.Y},{triangle.P1.Z}) " +
                          $"P2=({triangle.P2.X},{triangle.P2.Y},{triangle.P2.Z})");
        Console.WriteLine($"points ({points.Count}):");
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            Console.WriteLine($"  {i}: bary=({p.Barycentric.U},{p.Barycentric.V},{p.Barycentric.W}) pos=({p.Position.X},{p.Position.Y},{p.Position.Z})");
        }

        Console.WriteLine($"segments ({segments.Count}):");
        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            Console.WriteLine($"  {i}: {s.StartIndex} -> {s.EndIndex}");
        }

        if (ex != null)
        {
            Console.WriteLine("exception:");
            Console.WriteLine(ex);
        }
    }
}
