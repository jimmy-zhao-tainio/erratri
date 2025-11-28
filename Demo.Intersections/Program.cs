using Geometry;
using System;
using System.Linq;
using World;
using Topology;
using Kernel;
using IO;

namespace Demo.Intersections;

internal static class Program
{
    private sealed class TrianglePairShape : Shape
    {
        public TrianglePairShape(in Triangle first, in Triangle second)
        {
            Mesh = new Mesh(new[] { first, second });
        }
    }

    private static void Main()
    {
        SaveNoneExample();
        SavePointExample();
        SaveSegmentExample();
        SaveAreaExample();
        SaveSphereIntersectionExample();
        SaveTetraPeekExample();
    }

    private static void SaveNoneExample()
    {
        // Coplanar, disjoint triangles (no intersection).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(4, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        ValidateIntersection("none", in triA, in triB, IntersectionType.None);
        SavePair("intersection_none.stl", in triA, in triB);
    }

    private static void SavePointExample()
    {
        // One triangle lies in the plane z = 0. A second, vertical triangle
        // has a single vertex touching the interior of the first at its
        // centroid, so the intersection is exactly one point.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        // Centroid of triA is (2,2,0).
        var p = new Point(2, 2, 0);

        var b0 = p;                     // touching point
        var b1 = new Point(2, 2, 3);    // above the plane
        var b2 = new Point(2, 5, 3);    // above the plane, offset in Y

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(2, 2, -1));

        ValidateIntersection("point", in triA, in triB, IntersectionType.Point);
        SavePair("intersection_point.stl", in triA, in triB);
    }

    private static void SaveSegmentExample()
    {
        // Non-coplanar triangles intersecting along a segment on the y-axis
        // from (0,0,0) to (0,1,0).
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        ValidateIntersection("segment", in triA, in triB, IntersectionType.Segment);
        SavePair("intersection_segment.stl", in triA, in triB);
    }

    private static void SaveAreaExample()
    {
        // Coplanar triangles where one "cuts through" the other,
        // producing a clear area overlap that is not containment.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(-2, 4, 0);
        var b1 = new Point(4, -2, 0);
        var b2 = new Point(8, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        ValidateIntersection("area", in triA, in triB, IntersectionType.Area);
        SavePair("intersection_area.stl", in triA, in triB);
    }

    private static void SaveSphereIntersectionExample()
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var sphereA = new Sphere(r, subdivisions: 3, center: aCenter);
        var sphereB = new Sphere(r, subdivisions: 3, center: bCenter);

        var set = new IntersectionSet(
            sphereA.Mesh.Triangles,
            sphereB.Mesh.Triangles);

        var involved = new List<Triangle>();
        var seenA = new HashSet<int>();
        var seenB = new HashSet<int>();

        foreach (var intersection in set.Intersections)
        {
            if (seenA.Add(intersection.TriangleIndexA))
            {
                involved.Add(set.TrianglesA[intersection.TriangleIndexA]);
            }

            if (seenB.Add(intersection.TriangleIndexB))
            {
                involved.Add(set.TrianglesB[intersection.TriangleIndexB]);
            }
        }

        var outputPath = "spheres_intersection_set.stl";
        StlWriter.Write(involved, outputPath);
    }

    private static void SaveTetraPeekExample()
    {
        // Tetrahedron A with face ABC on the plane z = 0.
        var aA = new Point(0, 0, 0);
        var aB = new Point(4, 0, 0);
        var aC = new Point(0, 4, 0);
        var aD = new Point(0, 0, 4);
        var tetraA = new World.Tetrahedron(aA, aB, aC, aD);

        // Tetrahedron B positioned so that one vertex P lies above face ABC
        // strictly inside its interior, while the other vertices lie below
        // the plane z = 0. Edges from P to the other vertices pierce the
        // interior of ABC without touching its edges.
        var bP = new Point(1, 1, 1);      // inside ABC, above the plane
        var bQ = new Point(-1, -1, -2);
        var bR = new Point(5, -1, -2);
        var bS = new Point(-1, 5, -2);
        var tetraB = new World.Tetrahedron(bP, bQ, bR, bS);

        var set = new IntersectionSet(tetraA.Mesh.Triangles, tetraB.Mesh.Triangles);
        var counts = set.Intersections
            .GroupBy(i => i.Type)
            .Select(g => $"{g.Key}:{g.Count()}");

        Console.WriteLine("Tetra peek example:");
        Console.WriteLine($"  intersecting pairs = {set.Intersections.Count} ({string.Join(", ", counts)})");

        var graph = IntersectionGraph.FromIntersectionSet(set);
        Console.WriteLine($"  graph vertices = {graph.Vertices.Count}, edges = {graph.Edges.Count}");

        RunTriangleSubdivisionCheck(set, graph);

        var world = new World.World();
        world.Add(tetraA);
        world.Add(tetraB);
        world.Save("tetra_peek.stl");
    }

    private static void ValidateIntersection(string label, in Triangle first, in Triangle second, IntersectionType expected)
    {
        var actual = IntersectionTypes.Classify(in first, in second);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Demo '{label}' expected {expected} but Intersection.Classify returned {actual}.");
        }
    }

    private static void SavePair(string path, in Triangle first, in Triangle second)
    {
        var world = new World.World();
        world.Add(new TrianglePairShape(in first, in second));
        world.Save(path);
    }

    private sealed class TriAccum
    {
        public Triangle Triangle { get; }
        public List<TriangleSubdivision.IntersectionPoint> Points { get; } = new();
        public List<TriangleSubdivision.IntersectionSegment> Segments { get; } = new();
        public List<RealPoint> WorldPoints { get; } = new();

        public TriAccum(Triangle triangle)
        {
            Triangle = triangle;
        }
    }

    private static void RunTriangleSubdivisionCheck(IntersectionSet set, IntersectionGraph graph)
    {
        var triA = set.TrianglesA ?? throw new ArgumentNullException(nameof(set.TrianglesA));
        var triB = set.TrianglesB ?? throw new ArgumentNullException(nameof(set.TrianglesB));

        var accums = new Dictionary<(char mesh, int idx), TriAccum>();

        TriAccum GetAccum(char mesh, int idx, Triangle tri)
        {
            var key = (mesh, idx);
            if (!accums.TryGetValue(key, out var acc))
            {
                acc = new TriAccum(tri);
                accums[key] = acc;
            }
            return acc;
        }

        foreach (var pair in graph.Pairs.Select((p, i) => (p, i)))
        {
            var intersection = pair.p.Intersection;
            var triAIdx = intersection.TriangleIndexA;
            var triBIdx = intersection.TriangleIndexB;

            var accA = GetAccum('A', triAIdx, triA[triAIdx]);
            var accB = GetAccum('B', triBIdx, triB[triBIdx]);

            var mapA = new Dictionary<int, int>();
            var mapB = new Dictionary<int, int>();

            foreach (var v in pair.p.Vertices)
            {
                int idxA = GetOrAddPoint(accA, v.OnTriangleA, accA.Triangle);
                int idxB = GetOrAddPoint(accB, v.OnTriangleB, accB.Triangle);
                mapA[v.VertexId.Value] = idxA;
                mapB[v.VertexId.Value] = idxB;
            }

            foreach (var seg in pair.p.Segments)
            {
                if (mapA.TryGetValue(seg.Start.VertexId.Value, out var sa) &&
                    mapA.TryGetValue(seg.End.VertexId.Value, out var ea))
                {
                    AddSegment(accA, sa, ea);
                }

                if (mapB.TryGetValue(seg.Start.VertexId.Value, out var sb) &&
                    mapB.TryGetValue(seg.End.VertexId.Value, out var eb))
                {
                    AddSegment(accB, sb, eb);
                }
            }
        }

        Console.WriteLine("Triangle subdivision checks:");
        foreach (var kvp in accums)
        {
            var key = kvp.Key;
            var acc = kvp.Value;
            if (acc.Segments.Count == 0)
            {
                continue;
            }

            var triVal = acc.Triangle;
            var patches = TriangleSubdivision.Subdivide(in triVal, acc.Points, acc.Segments);
            double triArea = Math.Abs(new RealTriangle(
                new RealPoint(triVal.P0),
                new RealPoint(triVal.P1),
                new RealPoint(triVal.P2)).SignedArea);

            double patchSum = 0.0;
            foreach (var p in patches)
            {
                patchSum += Math.Abs(new RealTriangle(p.P0, p.P1, p.P2).SignedArea);
            }

            double diff = Math.Abs(patchSum - triArea);
            double relTol = Tolerances.BarycentricInsideEpsilon * triArea;
            bool ok = diff <= Tolerances.EpsArea || diff <= relTol;

            Console.WriteLine($"  {key.mesh}{key.idx}: points={acc.Points.Count}, segments={acc.Segments.Count}, patches={patches.Count}, area orig={triArea}, patches={patchSum}, diff={diff}, ok={ok}");
        }
    }

    private static int GetOrAddPoint(TriAccum acc, Barycentric bary, Triangle tri)
    {
        var pos = tri.FromBarycentric(in bary);
        for (int i = 0; i < acc.WorldPoints.Count; i++)
        {
            var wp = acc.WorldPoints[i];
            double d2 = wp.DistanceSquared(in pos);
            if (d2 <= Tolerances.FeatureWorldDistanceEpsilonSquared)
            {
                return i;
            }
        }

        acc.WorldPoints.Add(pos);
        acc.Points.Add(new TriangleSubdivision.IntersectionPoint(bary, pos));
        return acc.WorldPoints.Count - 1;
    }

    private static void AddSegment(TriAccum acc, int a, int b)
    {
        if (a == b) return;
        int lo = Math.Min(a, b);
        int hi = Math.Max(a, b);
        foreach (var s in acc.Segments)
        {
            int sa = Math.Min(s.StartIndex, s.EndIndex);
            int sb = Math.Max(s.StartIndex, s.EndIndex);
            if (sa == lo && sb == hi)
            {
                return;
            }
        }
        acc.Segments.Add(new TriangleSubdivision.IntersectionSegment(a, b));
    }
}
