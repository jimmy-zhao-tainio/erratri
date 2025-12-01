using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates;

namespace Kernel.Pslg.Phases;

internal static class PslgTriangulationPhase
{
    // Phase #5: map triangulated UV faces back to world-space patches.
    internal static PslgTriangulationState Run(
        Triangle triangle,
        PslgSelectionState selectionState)
    {
        if (selectionState.Vertices is null) throw new ArgumentNullException(nameof(selectionState));
        if (selectionState.Selection.InteriorFaces is null) throw new ArgumentNullException(nameof(selectionState));

        var patches = new List<RealTriangle>();

        RealPoint MapVertex(int idx)
        {
            double u = selectionState.Vertices[idx].X;
            double v = selectionState.Vertices[idx].Y;
            double w = 1.0 - u - v;
            var bary = new Barycentric(u, v, w);
            return Barycentric.ToRealPointOnTriangle(in triangle, in bary);
        }

        foreach (var face in selectionState.Selection.InteriorFaces)
        {
            var tris = TriangulateFace(in selectionState, face);

            double uvTriSum = 0.0;
            foreach (var t in tris)
            {
                double areaUv = new RealTriangle(
                    new RealPoint(selectionState.Vertices[t.A].X, selectionState.Vertices[t.A].Y, 0.0),
                    new RealPoint(selectionState.Vertices[t.B].X, selectionState.Vertices[t.B].Y, 0.0),
                    new RealPoint(selectionState.Vertices[t.C].X, selectionState.Vertices[t.C].Y, 0.0)).SignedArea;
                uvTriSum += areaUv;
            }

            double uvExpected = Math.Abs(face.SignedAreaUV);
            double uvDiff = Math.Abs(uvTriSum - uvExpected);
            double uvRel = Tolerances.BarycentricInsideEpsilon * uvExpected;
            if (uvDiff > Tolerances.EpsArea && uvDiff > uvRel)
            {
                throw new InvalidOperationException(
                    $"Face triangulation area mismatch: faceArea={face.SignedAreaUV}, triSum={uvTriSum}, outer={string.Join(",", face.OuterVertices)}");
            }

            foreach (var t in tris)
            {
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

        return new PslgTriangulationState(
            selectionState.Vertices,
            selectionState.Edges,
            selectionState.HalfEdges,
            selectionState.Faces,
            selectionState.Selection,
            patches);
    }

    private static List<(int A, int B, int C)> TriangulateFace(
        in PslgSelectionState selectionState,
        PslgFace face)
    {
        var vertices = selectionState.Vertices;
        if (vertices is null) throw new ArgumentNullException(nameof(selectionState));
        if (face.OuterVertices is null || face.OuterVertices.Length < 3)
        {
            throw new ArgumentException("Face must have an outer boundary with at least 3 vertices.", nameof(face));
        }

        if (face.InteriorCycles.Count == 0)
        {
            return TriangulateSimple(face.OuterVertices, vertices, face.SignedAreaUV);
        }

        return TriangulateWithInteriorCycles(face, vertices);
    }

    internal static List<(int A, int B, int C)> TriangulateSimple(
        int[] polygon,
        IReadOnlyList<PslgVertex> vertices,
        double expectedArea)
    {
        var polyList = new List<int>(polygon);
        if (polyList.Count < 3)
        {
            throw new InvalidOperationException("Face must have at least 3 vertices.");
        }

        polyList = SimplifyPolygonRing(polyList, vertices);
        if (polyList.Count < 3)
        {
            throw new InvalidOperationException("Face must have at least 3 vertices after simplification.");
        }

        // Ensure CCW orientation.
        var faceCoords = new List<RealPoint>(polyList.Count);
        foreach (var idx in polyList)
        {
            faceCoords.Add(new RealPoint(vertices[idx].X, vertices[idx].Y, 0.0));
        }
        double targetSignedArea = new RealPolygon(faceCoords).SignedArea;
        if (targetSignedArea < 0)
        {
            polyList.Reverse();
            faceCoords.Reverse();
            targetSignedArea = -targetSignedArea;
        }

        var triangles = new List<(int A, int B, int C)>(polyList.Count - 2);

        if (IsConvexOrCollinear(polyList, vertices))
        {
            int anchor = polyList[0];
            int last = polyList[1];
            for (int i = 2; i < polyList.Count; i++)
            {
                int c = polyList[i];
                double area = new RealTriangle(
                    new RealPoint(vertices[anchor].X, vertices[anchor].Y, 0.0),
                    new RealPoint(vertices[last].X, vertices[last].Y, 0.0),
                    new RealPoint(vertices[c].X, vertices[c].Y, 0.0)).SignedArea;
                if (area <= Tolerances.EpsArea)
                {
                    // Skip collinear/degenerate step; move last forward.
                    last = c;
                    continue;
                }

                triangles.Add((anchor, last, c));
                last = c;
            }
        }
        else
        {
            while (polyList.Count > 3)
            {
                bool earFound = false;
                int n = polyList.Count;

                for (int i = 0; i < n; i++)
                {
                    int prev = polyList[(i - 1 + n) % n];
                    int curr = polyList[i];
                    int next = polyList[(i + 1) % n];

                    double area = new RealTriangle(
                        new RealPoint(vertices[prev].X, vertices[prev].Y, 0.0),
                        new RealPoint(vertices[curr].X, vertices[curr].Y, 0.0),
                        new RealPoint(vertices[next].X, vertices[next].Y, 0.0)).SignedArea;
                    if (area <= Tolerances.EpsArea)
                    {
                        continue; // not strictly convex or degenerate
                    }

                    bool anyInside = false;
                    for (int k = 0; k < n; k++)
                    {
                        if (k == (i - 1 + n) % n || k == i || k == (i + 1) % n)
                        {
                            continue;
                        }

                        if (RealTrianglePredicates.IsInsideStrict(
                                  new RealTriangle(
                                      new RealPoint(vertices[prev].X, vertices[prev].Y, 0.0),
                                      new RealPoint(vertices[curr].X, vertices[curr].Y, 0.0),
                                      new RealPoint(vertices[next].X, vertices[next].Y, 0.0)),
                                new RealPoint(vertices[polyList[k]].X, vertices[polyList[k]].Y, 0.0)))
                        {
                            anyInside = true;
                            break;
                        }
                    }

                    if (anyInside)
                    {
                        continue;
                    }

                    triangles.Add((prev, curr, next));
                    polyList.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    throw new InvalidOperationException("Ear clipping failed: no valid ear found for a non-triangular polygon.");
                }
            }

            triangles.Add((polyList[0], polyList[1], polyList[2]));
        }

        double sumArea = 0.0;
        foreach (var t in triangles)
        {
            double area = new RealTriangle(
                new RealPoint(vertices[t.A].X, vertices[t.A].Y, 0.0),
                new RealPoint(vertices[t.B].X, vertices[t.B].Y, 0.0),
                new RealPoint(vertices[t.C].X, vertices[t.C].Y, 0.0)).SignedArea;
            if (area <= Tolerances.EpsArea)
            {
                throw new InvalidOperationException("Ear clipping produced a non-positive area triangle.");
            }

            var centroid = new RealPoint(
                (vertices[t.A].X + vertices[t.B].X + vertices[t.C].X) / 3.0,
                (vertices[t.A].Y + vertices[t.B].Y + vertices[t.C].Y) / 3.0,
                0.0);

            if (!RealPolygonPredicates.ContainsInclusive(new RealPolygon(faceCoords), centroid))
            {
                throw new InvalidOperationException("Triangle centroid lies outside the parent polygon.");
            }

            sumArea += area;
        }

        double absExpected = Math.Abs(expectedArea);
        double diffAbs = Math.Abs(sumArea - absExpected);
        double rel = Tolerances.BarycentricInsideEpsilon * absExpected;
        if (diffAbs > Tolerances.EpsArea && diffAbs > rel)
        {
            throw new InvalidOperationException("Ear clipping area check failed for face.");
        }

        return triangles;
    }

    private static List<int> SimplifyPolygonRing(List<int> polygon, IReadOnlyList<PslgVertex> vertices)
    {
        if (polygon.Count <= 3)
        {
            return polygon;
        }

        var simplified = new List<int>(polygon.Count);
        int n = polygon.Count;

        for (int i = 0; i < n; i++)
        {
            int prev = polygon[(i - 1 + n) % n];
            int curr = polygon[i];
            int next = polygon[(i + 1) % n];

            if (curr == prev || curr == next)
            {
                continue;
            }

            simplified.Add(curr);
        }

        return simplified.Count >= 3 ? simplified : polygon;
    }

    private static bool IsConvexOrCollinear(List<int> polyList, IReadOnlyList<PslgVertex> vertices)
    {
        int n = polyList.Count;
        for (int i = 0; i < n; i++)
        {
            int prev = polyList[(i - 1 + n) % n];
            int curr = polyList[i];
            int next = polyList[(i + 1) % n];

            double ax = vertices[curr].X - vertices[prev].X;
            double ay = vertices[curr].Y - vertices[prev].Y;
            double bx = vertices[next].X - vertices[curr].X;
            double by = vertices[next].Y - vertices[curr].Y;
            double cross = ax * by - ay * bx;

            if (cross < -Tolerances.EpsArea)
            {
                return false; // reflex
            }
        }

        return true;
    }

    private static List<(int A, int B, int C)> TriangulateWithInteriorCycles(PslgFace face, IReadOnlyList<PslgVertex> vertices)
    {
        // Build visibility-tested bridges between the outer ring and any
        // interior cycles, stitching them into a simple polygon.
        var stitched = StitchInteriorCycles(face, vertices);
        return TriangulateSimple(stitched.ToArray(), vertices, face.SignedAreaUV);
    }

    private static List<int> StitchInteriorCycles(PslgFace face, IReadOnlyList<PslgVertex> vertices)
    {
        var polygon = new List<int>(face.OuterVertices);

        // Existing segments set for visibility tests.
        var segments = new List<(int A, int B)>();
        void AddCycleSegments(int[] cyc)
        {
            for (int i = 0; i < cyc.Length; i++)
            {
                int a = cyc[i];
                int b = cyc[(i + 1) % cyc.Length];
                segments.Add((a, b));
            }
        }

        AddCycleSegments(face.OuterVertices);
        foreach (var interiorCycle in face.InteriorCycles)
        {
            AddCycleSegments(interiorCycle);
        }

        var uniqueInteriorCycles = new List<int[]>(face.InteriorCycles.Count);
        var interiorCycleKeys = new HashSet<string>();
        foreach (var cycle in face.InteriorCycles)
        {
            var key = PslgSelectionPhase.CanonicalFaceKey(cycle);
            if (interiorCycleKeys.Add(key))
            {
                uniqueInteriorCycles.Add(cycle);
            }
        }

        foreach (var interiorCycle in uniqueInteriorCycles)
        {
            if (interiorCycle.Length < 3) continue;

            // Pick interior-cycle vertex with smallest (x,y).
            int hIndex = 0;
            for (int i = 1; i < interiorCycle.Length; i++)
            {
                var vh = vertices[interiorCycle[i]];
                var vbest = vertices[interiorCycle[hIndex]];
                if (vh.X < vbest.X - Tolerances.EpsVertex ||
                    (Math.Abs(vh.X - vbest.X) <= Tolerances.EpsVertex && vh.Y < vbest.Y))
                {
                    hIndex = i;
                }
            }
            int hVertex = interiorCycle[hIndex];

            int bestOuterIdx = -1;
            double bestDist2 = double.MaxValue;
            for (int i = 0; i < polygon.Count; i++)
            {
                int o = polygon[i];
                if (IsBridgeVisible(vertices, segments, o, hVertex))
                {
                    double dx = vertices[o].X - vertices[hVertex].X;
                    double dy = vertices[o].Y - vertices[hVertex].Y;
                    double d2 = dx * dx + dy * dy;
                    if (d2 < bestDist2)
                    {
                        bestDist2 = d2;
                        bestOuterIdx = i;
                    }
                }
            }

            if (bestOuterIdx < 0)
            {
                throw new InvalidOperationException("Failed to find a visible bridge from interior cycle to outer boundary.");
            }

            // Build stitched path:
            // outer[0..bestOuterIdx], bridge to h, traverse interior cycle CW from h back to h,
            // bridge back to outer[bestOuterIdx], then continue outer.
            var stitched = new List<int>(polygon.Count + interiorCycle.Length + 3);
            for (int i = 0; i <= bestOuterIdx; i++)
            {
                stitched.Add(polygon[i]);
            }

            stitched.Add(hVertex); // enter interior cycle

            for (int k = 1; k < interiorCycle.Length; k++)
            {
                int idx = (hIndex - k + interiorCycle.Length) % interiorCycle.Length; // CW order
                stitched.Add(interiorCycle[idx]);
            }

            stitched.Add(hVertex); // exit interior cycle
            stitched.Add(polygon[bestOuterIdx]); // bridge back to outer

            for (int i = bestOuterIdx + 1; i < polygon.Count; i++)
            {
                stitched.Add(polygon[i]);
            }

            polygon = stitched;

            // Rebuild segments with the new polygon cycle (outer edges plus interior-cycle perimeter and bridges).
            segments.Clear();
            AddCycleSegments(polygon.ToArray());
        }

        // Compress consecutive duplicates, including a repeated start/end.
        var compressed = new List<int>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            int curr = polygon[i];
            if (compressed.Count > 0 && compressed[^1] == curr)
            {
                continue;
            }
            compressed.Add(curr);
        }

        if (compressed.Count > 1 && compressed[0] == compressed[^1])
        {
            compressed.RemoveAt(compressed.Count - 1);
        }

        polygon = compressed;

        // Degenerate after compression?
        var distinct = new HashSet<int>(polygon);
        if (polygon.Count < 3 || distinct.Count < 3)
        {
            throw new InvalidOperationException($"Stitched polygon degenerated after compression: {string.Join("->", polygon)}");
        }

        // Sanity checks: no immediate duplicates.
        for (int i = polygon.Count - 1, j = 0; j < polygon.Count; i = j, j++)
        {
            if (polygon[i] == polygon[j])
            {
                throw new InvalidOperationException($"Stitched polygon has consecutive duplicate vertices at indices {i}->{j}: {polygon[i]}. Polygon: {string.Join("->", polygon)}");
            }
        }

        // Self-intersection check.
        if (HasSelfIntersection(polygon, vertices))
        {
            throw new InvalidOperationException($"Stitched polygon self-intersects. Polygon: {string.Join("->", polygon)}");
        }

        // Area check against expected ring area.
        var polyPoints = new List<RealPoint>(polygon.Count);
        foreach (var idx in polygon)
        {
            var v = vertices[idx];
            polyPoints.Add(new RealPoint(v.X, v.Y, 0.0));
        }
        double area = new RealPolygon(polyPoints).SignedArea;
        double absArea = Math.Abs(area);
        double expected = Math.Abs(face.SignedAreaUV);
        double diff = Math.Abs(absArea - expected);
        double rel = Tolerances.BarycentricInsideEpsilon * expected;
        if (diff > Tolerances.EpsArea && diff > rel)
        {
            throw new InvalidOperationException(
                $"Stitched polygon area mismatch: stitched={absArea}, expected={expected}, poly={string.Join("->", polygon)}");
        }

        return polygon;
    }

    private static bool HasSelfIntersection(List<int> poly, IReadOnlyList<PslgVertex> vertices)
    {
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            int a0 = poly[i];
            int a1 = poly[(i + 1) % n];
            var segA = new RealSegment(
                new RealPoint(vertices[a0].X, vertices[a0].Y, 0.0),
                new RealPoint(vertices[a1].X, vertices[a1].Y, 0.0));

            for (int j = i + 2; j < n; j++)
            {
                int b0 = poly[j];
                int b1 = poly[(j + 1) % n];

                // Skip adjacent edges and edges sharing a vertex.
                if (a0 == b0 || a0 == b1 || a1 == b0 || a1 == b1)
                {
                    continue;
                }

                // Skip the check for the last edge against the first edge adjacency.
                if (i == 0 && j == n - 1)
                {
                    continue;
                }

                var segB = new RealSegment(
                    new RealPoint(vertices[b0].X, vertices[b0].Y, 0.0),
                    new RealPoint(vertices[b1].X, vertices[b1].Y, 0.0));

                if (RealSegmentPredicates.TryIntersect(segA, segB, out var inter))
                {
                    if (!IsNearVertex(vertices, inter.X, inter.Y, a0, a1) &&
                        !IsNearVertex(vertices, inter.X, inter.Y, b0, b1))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsBridgeVisible(
        IReadOnlyList<PslgVertex> vertices,
        List<(int A, int B)> segments,
        int va,
        int vb)
    {
        var seg = new RealSegment(
            new RealPoint(vertices[va].X, vertices[va].Y, 0.0),
            new RealPoint(vertices[vb].X, vertices[vb].Y, 0.0));

        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];

            if (s.A == va || s.A == vb || s.B == va || s.B == vb)
            {
                continue;
            }

            var existing = new RealSegment(
                new RealPoint(vertices[s.A].X, vertices[s.A].Y, 0.0),
                new RealPoint(vertices[s.B].X, vertices[s.B].Y, 0.0));

            if (RealSegmentPredicates.TryIntersect(seg, existing, out var inter))
            {
                // Allow touching very near an existing vertex; otherwise reject.
                if (!IsNearVertex(vertices, inter.X, inter.Y, va, vb))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsNearVertex(
        IReadOnlyList<PslgVertex> vertices,
        double x,
        double y,
        int va,
        int vb)
    {
        double eps2 = Tolerances.PslgVertexMergeEpsilonSquared;
        var a = vertices[va];
        var b = vertices[vb];
        double da = (a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y);
        double db = (b.X - x) * (b.X - x) + (b.Y - y) * (b.Y - y);
        return da <= eps2 || db <= eps2;
    }
}
