using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Geometry.Predicates;
using Kernel;
using IO;

namespace Kernel.Fuzz.BoxTetFuzz;

internal static class Program
{
    private sealed record Tet(Point A, Point B, Point C, Point D)
    {
        public IEnumerable<(Triangle tri, Point missing)> Faces()
        {
            // Each face uses the opposite vertex as the "missing" point to orient the triangle.
            yield return (new Triangle(A, B, C, D), D);
            yield return (new Triangle(A, C, D, B), B);
            yield return (new Triangle(A, D, B, C), C);
            yield return (new Triangle(B, D, C, A), A);
        }
    }

    private sealed class BoxSolid
    {
        public IReadOnlyList<Tet> Tets { get; }

        public BoxSolid(double min, double max)
        {
            // Axis-aligned box [min,max]^3 tessellated into 5 tetrahedra.
            var p000 = ToPoint(min, min, min);
            var p100 = ToPoint(max, min, min);
            var p010 = ToPoint(min, max, min);
            var p110 = ToPoint(max, max, min);
            var p001 = ToPoint(min, min, max);
            var p101 = ToPoint(max, min, max);
            var p011 = ToPoint(min, max, max);
            var p111 = ToPoint(max, max, max);

            Tets = new List<Tet>
            {
                new(p000, p100, p010, p001),
                new(p100, p110, p010, p111),
                new(p100, p010, p001, p111),
                new(p010, p001, p011, p111),
                new(p100, p001, p101, p111)
            };
        }
    }

    private static void Main(string[] args)
    {
        int iterations = 1_000_000;
        int seed = 12345;
        double minVolumeEps = 1e-6;

        if (args.Length > 0 && int.TryParse(args[0], out var iters))
        {
            iterations = iters;
        }

        if (args.Length > 1 && int.TryParse(args[1], out var s))
        {
            seed = s;
        }

        if (args.Length > 2 && double.TryParse(args[2], out var eps))
        {
            minVolumeEps = eps;
        }

        Console.WriteLine($"BoxTet fuzz: iterations={iterations}, seed={seed}, minVolume={minVolumeEps}");

        var rng = new Random(seed);
        double outerMin = 0.0;
        double outerMax = 100.0;
        double innerMin = 25.0;
        double innerMax = 75.0;
        var innerBox = new BoxSolid(innerMin, innerMax);

        for (int iter = 0; iter < iterations; iter++)
        {
            var tet = RandomTet(rng, outerMin, outerMax, innerMin, innerMax, minVolumeEps);

            try
            {
                RunOne(iter, tet, innerBox);
            }
            catch (Exception ex)
            {
                Console.WriteLine("---- Fuzz failure ----");
                Console.WriteLine($"iteration={iter}");
                Console.WriteLine("tet vertices:");
                Console.WriteLine($"  A=({tet.A.X},{tet.A.Y},{tet.A.Z})");
                Console.WriteLine($"  B=({tet.B.X},{tet.B.Y},{tet.B.Z})");
                Console.WriteLine($"  C=({tet.C.X},{tet.C.Y},{tet.C.Z})");
                Console.WriteLine($"  D=({tet.D.X},{tet.D.Y},{tet.D.Z})");
                Console.WriteLine(ex);

                var failTris = new List<Triangle>();
                foreach (var bt in innerBox.Tets)
                {
                    failTris.AddRange(bt.Faces().Select(f => f.tri));
                }
                failTris.AddRange(tet.Faces().Select(f => f.tri));

                var stlPath = $"box_tet_fail_iter{iter}.stl";
                StlWriter.Write(failTris, stlPath);
                Console.WriteLine($"Saved fail geometry to {stlPath}");
                return;
            }

            if (iter % 1000 == 0)
            {
                Console.Write($"\r  iter {iter}/{iterations}");
            }
        }

        Console.WriteLine($"\rFuzz completed successfully. iterations={iterations}   ");
    }

    private static Tet RandomTet(Random rng, double min, double max, double innerMin, double innerMax, double minVolumeEps)
    {
        while (true)
        {
            var p0 = RandomPoint(rng, min, max);
            var p1 = RandomPoint(rng, min, max);
            var p2 = RandomPoint(rng, min, max);
            var p3 = RandomPoint(rng, min, max);

            double volume = Math.Abs(ComputeSignedVolume(p0, p1, p2, p3));
            if (volume < minVolumeEps)
            {
                continue; // degenerate
            }

            // Reject if all inside inner box.
            bool Inside(Point p) => p.X > innerMin && p.X < innerMax && p.Y > innerMin && p.Y < innerMax && p.Z > innerMin && p.Z < innerMax;
            if (Inside(p0) && Inside(p1) && Inside(p2) && Inside(p3))
            {
                continue;
            }

            return new Tet(p0, p1, p2, p3);
        }
    }

    private static Point RandomPoint(Random rng, double min, double max)
    {
        double x = min + rng.NextDouble() * (max - min);
        double y = min + rng.NextDouble() * (max - min);
        double z = min + rng.NextDouble() * (max - min);
        return ToPoint(x, y, z);
    }

    private static Point ToPoint(double x, double y, double z)
    {
        long lx = (long)Math.Round(x);
        long ly = (long)Math.Round(y);
        long lz = (long)Math.Round(z);
        return new Point(lx, ly, lz);
    }

    private static double ComputeSignedVolume(Point a, Point b, Point c, Point d)
    {
        double v0x = b.X - a.X;
        double v0y = b.Y - a.Y;
        double v0z = b.Z - a.Z;
        double v1x = c.X - a.X;
        double v1y = c.Y - a.Y;
        double v1z = c.Z - a.Z;
        double v2x = d.X - a.X;
        double v2y = d.Y - a.Y;
        double v2z = d.Z - a.Z;
        return (v0x * (v1y * v2z - v1z * v2y)
              - v0y * (v1x * v2z - v1z * v2x)
              + v0z * (v1x * v2y - v1y * v2x)) / 6.0;
    }

    private static void RunOne(int iteration, Tet randomTet, BoxSolid box)
    {
        foreach (var boxTet in box.Tets)
        {
            var facesA = boxTet.Faces().ToArray();
            var facesB = randomTet.Faces().ToArray();

            for (int i = 0; i < facesA.Length; i++)
            {
                for (int j = 0; j < facesB.Length; j++)
                {
                    var triA = facesA[i].tri;
                    var triB = facesB[j].tri;

                    var type = IntersectionTypes.Classify(in triA, in triB);
                    if (type == IntersectionType.None)
                    {
                        continue;
                    }

                    var set = BuildIntersectionSet(triA, triB);
                    var pair = new IntersectionSet.Intersection(0, 0, type);
                    var features = PairFeaturesFactory.Create(in set, in pair);

                    ValidateSubdivision(iteration, "A", triA, features, useTriangleA: true);
                    ValidateSubdivision(iteration, "B", triB, features, useTriangleA: false);
                }
            }
        }
    }

    private static IntersectionSet BuildIntersectionSet(Triangle triA, Triangle triB)
    {
        var listA = new List<Triangle> { triA };
        var listB = new List<Triangle> { triB };
        return new IntersectionSet(listA, listB);
    }

    private static void ValidateSubdivision(int iteration, string label, Triangle tri, PairFeatures features, bool useTriangleA)
    {
        var points = new List<TriangleSubdivision.IntersectionPoint>(features.Vertices.Count);
        var indexMap = new Dictionary<int, int>();

        for (int i = 0; i < features.Vertices.Count; i++)
        {
            var v = features.Vertices[i];
            var bary = useTriangleA ? v.OnTriangleA : v.OnTriangleB;
            var pos = tri.FromBarycentric(in bary);
            int mapped = AddOrGetPoint(points, pos, bary);
            indexMap[v.VertexId.Value] = mapped;
        }

        var segments = new List<TriangleSubdivision.IntersectionSegment>(features.Segments.Count);
        foreach (var seg in features.Segments)
        {
            if (!indexMap.TryGetValue(seg.Start.VertexId.Value, out var sIdx) ||
                !indexMap.TryGetValue(seg.End.VertexId.Value, out var eIdx))
            {
                continue;
            }
            segments.Add(new TriangleSubdivision.IntersectionSegment(sIdx, eIdx));
        }

        if (points.Count <= 2)
        {
            return; // treat as degenerate intersection for this fuzz
        }

        // If any intersection point lies strictly in the interior of this triangle, skip this pair for now.
        for (int i = 0; i < points.Count; i++)
        {
            if (TriangleSubdivision.ClassifyEdge(points[i].Barycentric) == TriangleSubdivision.EdgeLocation.Interior)
            {
                return;
            }
        }

        if (points.Count < 2 || segments.Count == 0)
        {
            // Nothing meaningful to subdivide.
            return;
        }

        IReadOnlyList<RealTriangle> patches;
        try
        {
            patches = TriangleSubdivision.Subdivide(in tri, points, segments);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Subdivision failure on triangle {label} at iter {iteration}");
            Console.WriteLine($"  tri: P0=({tri.P0.X},{tri.P0.Y},{tri.P0.Z}) P1=({tri.P1.X},{tri.P1.Y},{tri.P1.Z}) P2=({tri.P2.X},{tri.P2.Y},{tri.P2.Z})");
            Console.WriteLine($"  points ({points.Count}):");
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                Console.WriteLine($"    {i}: bary=({p.Barycentric.U},{p.Barycentric.V},{p.Barycentric.W}) pos=({p.Position.X},{p.Position.Y},{p.Position.Z})");
            }
            Console.WriteLine($"  segments ({segments.Count}):");
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                Console.WriteLine($"    {i}: {s.StartIndex} -> {s.EndIndex}");
            }
            throw new InvalidOperationException(
                $"Subdivision threw on triangle {label} at iter {iteration}: {ex.Message}", ex);
        }

        double triArea = Math.Abs(new RealTriangle(
            new RealPoint(tri.P0),
            new RealPoint(tri.P1),
            new RealPoint(tri.P2)).SignedArea3D);

        double patchSum = 0.0;
        foreach (var p in patches)
        {
            double area = Math.Abs(new RealTriangle(p.P0, p.P1, p.P2).SignedArea3D);
            if (area <= 0)
            {
                Console.WriteLine($"Non-positive patch area on triangle {label} at iter {iteration}");
                Console.WriteLine($"  tri: P0=({tri.P0.X},{tri.P0.Y},{tri.P0.Z}) P1=({tri.P1.X},{tri.P1.Y},{tri.P1.Z}) P2=({tri.P2.X},{tri.P2.Y},{tri.P2.Z})");
                for (int i = 0; i < patches.Count; i++)
                {
                    var rt = patches[i];
                    double a = new RealTriangle(rt.P0, rt.P1, rt.P2).SignedArea3D;
                    Console.WriteLine($"    patch {i}: area3D={a} pts=({rt.P0.X},{rt.P0.Y},{rt.P0.Z}) ({rt.P1.X},{rt.P1.Y},{rt.P1.Z}) ({rt.P2.X},{rt.P2.Y},{rt.P2.Z})");
                }
                throw new InvalidOperationException($"Patch has non-positive area on triangle {label} at iter {iteration}.");
            }
            patchSum += area;
        }

        double diff = Math.Abs(patchSum - triArea);
        double relTol = Tolerances.BarycentricInsideEpsilon * triArea;
        if (diff > Tolerances.EpsArea && diff > relTol)
        {
            throw new InvalidOperationException(
                $"Area mismatch on triangle {label} at iter {iteration}: tri={triArea}, patches={patchSum}, diff={diff}");
        }
    }

    private static int AddOrGetPoint(List<TriangleSubdivision.IntersectionPoint> points, RealPoint pos, Barycentric bary)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var existing = points[i].Position;
            double dx = existing.X - pos.X;
            double dy = existing.Y - pos.Y;
            double dz = existing.Z - pos.Z;
            double d2 = dx * dx + dy * dy + dz * dz;
            if (d2 <= Tolerances.FeatureWorldDistanceEpsilonSquared)
            {
                return i;
            }
        }

        points.Add(new TriangleSubdivision.IntersectionPoint(bary, pos));
        return points.Count - 1;
    }
}
