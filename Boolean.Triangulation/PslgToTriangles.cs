using System;
using System.Collections.Generic;
using ConstrainedTriangulator;
using Geometry;
using Geometry.Predicates;
using Pslg;

namespace Boolean;

// Triangulate PSLG output and map triangles back to world-space patches.
internal static class PslgToTriangles
{
    internal static IReadOnlyList<RealTriangle> Triangulate(
        in Triangle triangle,
        PslgOutput pslg)
    {
        if (pslg is null) throw new ArgumentNullException(nameof(pslg));

        var vertices = pslg.Vertices ?? throw new ArgumentNullException(nameof(pslg.Vertices));
        var edges = pslg.Edges ?? throw new ArgumentNullException(nameof(pslg.Edges));
        var interiorFaces = pslg.Selection.InteriorFaces ?? throw new ArgumentNullException(nameof(pslg.Selection));

        var uvPoints = new List<RealPoint2D>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            uvPoints.Add(new RealPoint2D(vertices[i].X, vertices[i].Y));
        }

        var segments = new List<(int A, int B)>(edges.Count);
        for (int i = 0; i < edges.Count; i++)
        {
            segments.Add((edges[i].Start, edges[i].End));
        }

        var ctInput = new Input(uvPoints, segments);
        var ctResult = Triangulator.RunFast(in ctInput, validate: false);

        if (ctResult.Points.Count != uvPoints.Count)
        {
            throw new InvalidOperationException(
                "ConstrainedTriangulator produced Steiner points; Kernel PSLG mapping requires a 1:1 vertex correspondence.");
        }

        var uvTriangles = OrientTrianglesCcw(ctResult.Triangles, ctResult.Points);

        var faceRegions = BuildFaceUvRegions(interiorFaces, vertices);
        var trianglesByFace = new List<(int A, int B, int C)>[faceRegions.Count];
        for (int i = 0; i < faceRegions.Count; i++)
        {
            trianglesByFace[i] = new List<(int A, int B, int C)>();
        }

        for (int ti = 0; ti < uvTriangles.Count; ti++)
        {
            var tri = uvTriangles[ti];
            var centroid = TriangleCentroid(ctResult.Points[tri.A], ctResult.Points[tri.B], ctResult.Points[tri.C]);

            for (int fi = 0; fi < faceRegions.Count; fi++)
            {
                if (IsInsideFace(in centroid, faceRegions[fi]))
                {
                    trianglesByFace[fi].Add(tri);
                    break;
                }
            }
        }

        var patches = new List<RealTriangle>();

        var triangleLocal = triangle;

        RealPoint MapVertex(int idx)
        {
            double u = vertices[idx].X;
            double v = vertices[idx].Y;
            double w = 1.0 - u - v;
            var bary = new Barycentric(u, v, w);
            return Barycentric.ToRealPointOnTriangle(in triangleLocal, in bary);
        }

        for (int fi = 0; fi < faceRegions.Count; fi++)
        {
            var face = faceRegions[fi].Face;
            var tris = trianglesByFace[fi];

            double uvTriSum = 0.0;
            for (int i = 0; i < tris.Count; i++)
            {
                var t = tris[i];
                uvTriSum += TriangleSignedArea(ctResult.Points[t.A], ctResult.Points[t.B], ctResult.Points[t.C]);
            }

            double uvExpected = faceRegions[fi].ExpectedArea;
            double uvDiff = Math.Abs(uvTriSum - uvExpected);
            double uvRel = Tolerances.BarycentricInsideEpsilon * uvExpected;
            if (uvDiff > Tolerances.EpsArea && uvDiff > uvRel)
            {
                throw new InvalidOperationException(
                    $"Face triangulation area mismatch: faceArea={face.SignedAreaUV}, triSum={uvTriSum}, outer={string.Join(",", face.OuterVertices)}");
            }

            for (int i = 0; i < tris.Count; i++)
            {
                var t = tris[i];

                var p0 = MapVertex(t.A);
                var p1 = MapVertex(t.B);
                var p2 = MapVertex(t.C);

                double area = new RealTriangle(p0, p1, p2).SignedArea3D;
                if (area <= 0)
                {
                    throw new InvalidOperationException("Mapped triangle has non-positive area in world space.");
                }

                patches.Add(new RealTriangle(p0, p1, p2));
            }
        }

        return patches;
    }

    private static List<(int A, int B, int C)> OrientTrianglesCcw(
        IReadOnlyList<(int A, int B, int C)> triangles,
        IReadOnlyList<RealPoint2D> points)
    {
        var oriented = new List<(int A, int B, int C)>(triangles.Count);

        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];
            int a = t.A;
            int b = t.B;
            int c = t.C;

            double area = TriangleSignedArea(points[a], points[b], points[c]);
            if (Math.Abs(area) <= Tolerances.EpsArea)
            {
                continue;
            }

            if (area < 0.0)
            {
                (b, c) = (c, b);
            }

            oriented.Add((a, b, c));
        }

        return oriented;
    }

    private static double TriangleSignedArea(in RealPoint2D a, in RealPoint2D b, in RealPoint2D c)
    {
        double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        return 0.5 * cross;
    }

    private static RealPoint TriangleCentroid(in RealPoint2D a, in RealPoint2D b, in RealPoint2D c)
    {
        return new RealPoint(
            (a.X + b.X + c.X) / 3.0,
            (a.Y + b.Y + c.Y) / 3.0,
            0.0);
    }

    private readonly struct FaceUvRegion
    {
        public PslgFace Face { get; }
        public RealPolygon Outer { get; }
        public IReadOnlyList<RealPolygon> Holes { get; }
        public double ExpectedArea { get; }

        public FaceUvRegion(
            PslgFace face,
            RealPolygon outer,
            IReadOnlyList<RealPolygon> holes,
            double expectedArea)
        {
            Face = face;
            Outer = outer;
            Holes = holes;
            ExpectedArea = expectedArea;
        }
    }

    private static List<FaceUvRegion> BuildFaceUvRegions(
        IReadOnlyList<PslgFace> faces,
        IReadOnlyList<PslgVertex> vertices)
    {
        var regions = new List<FaceUvRegion>(faces.Count);

        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            if (face.OuterVertices is null || face.OuterVertices.Length < 3)
            {
                throw new ArgumentException("Face must have an outer boundary with at least 3 vertices.", nameof(faces));
            }

            var outer = BuildPolygonCycle(face.OuterVertices, vertices);

            var holes = new List<RealPolygon>(face.InteriorCycles.Count);
            for (int j = 0; j < face.InteriorCycles.Count; j++)
            {
                var cyc = face.InteriorCycles[j];
                if (cyc is null || cyc.Length < 3)
                {
                    continue;
                }
                holes.Add(BuildPolygonCycle(cyc, vertices));
            }

            regions.Add(new FaceUvRegion(face, outer, holes, Math.Abs(face.SignedAreaUV)));
        }

        return regions;
    }

    private static RealPolygon BuildPolygonCycle(
        int[] cycle,
        IReadOnlyList<PslgVertex> vertices)
    {
        if (cycle is null) throw new ArgumentNullException(nameof(cycle));
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (cycle.Length < 3) throw new ArgumentException("Cycle must have at least 3 vertices.", nameof(cycle));

        var points = new List<RealPoint>(cycle.Length);

        int last = -1;
        for (int i = 0; i < cycle.Length; i++)
        {
            int idx = cycle[i];

            if (i == cycle.Length - 1 && idx == cycle[0])
            {
                break;
            }

            if (idx == last)
            {
                continue;
            }

            last = idx;
            var v = vertices[idx];
            points.Add(new RealPoint(v.X, v.Y, 0.0));
        }

        if (points.Count < 3)
        {
            throw new InvalidOperationException("Cycle degenerated after normalization.");
        }

        return new RealPolygon(points);
    }

    private static bool IsInsideFace(in RealPoint p, FaceUvRegion face)
    {
        if (!RealPolygonPredicates.ContainsInclusive(face.Outer, p))
        {
            return false;
        }

        for (int i = 0; i < face.Holes.Count; i++)
        {
            if (RealPolygonPredicates.ContainsInclusive(face.Holes[i], p))
            {
                return false;
            }
        }

        return true;
    }

    private static List<int> NormalizeRing(int[] ring)
    {
        if (ring.Length == 0)
        {
            return new List<int>();
        }

        int count = ring.Length;
        if (count > 1 && ring[0] == ring[^1])
        {
            count--;
        }

        var normalized = new List<int>(count);
        int last = -1;

        for (int i = 0; i < count; i++)
        {
            int idx = ring[i];
            if (idx == last)
            {
                continue;
            }

            normalized.Add(idx);
            last = idx;
        }

        if (normalized.Count > 1 && normalized[0] == normalized[^1])
        {
            normalized.RemoveAt(normalized.Count - 1);
        }

        if (normalized.Count < 3)
        {
            return normalized;
        }

        var seen = new HashSet<int>();
        for (int i = 0; i < normalized.Count; i++)
        {
            if (!seen.Add(normalized[i]))
            {
                throw new InvalidOperationException(
                    $"Polygon ring vertex index {normalized[i]} occurs more than once; ConstrainedTriangulator requires a simple ring.");
            }
        }

        return normalized;
    }
}
