using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Geometry.Predicates;

namespace Kernel;

// Internal PSLG (planar straight-line graph) scaffolding for per-triangle
// subdivision. This is a first step towards the general "PSLG lane" from
// TRIANGLESUBDIVISION-ROADMAP.md.
//
// Design notes:
//   - We work in the triangle's barycentric chart:
//       * Triangle corners map to (u, v) = (1,0), (0,1), (0,0).
//       * IntersectionPoint.Barycentric.U/V are used as local 2D coords.
//   - For now we:
//       * always add three triangle-corner vertices,
//       * add one vertex per IntersectionPoint,
//       * normalize and deduplicate vertices (Phase C),
//       * add boundary edges split at on-edge vertices,
//       * add PSLG edges per IntersectionSegment,
//       * verify no crossings without vertices (Phase D),
//       * build half-edges and extract faces (Phase E).

public readonly struct PslgVertex
{
    public double X { get; }
    public double Y { get; }

    // True if this vertex is one of the three triangle corners.
    public bool IsTriangleCorner { get; }

    // 0,1,2 for triangle corners; -1 otherwise.
    public int CornerIndex { get; }

    public PslgVertex(double x, double y, bool isTriangleCorner, int cornerIndex)
    {
        X = x;
        Y = y;
        IsTriangleCorner = isTriangleCorner;
        CornerIndex = cornerIndex;
    }
}

public readonly struct PslgEdge
{
    public int Start { get; }
    public int End { get; }

    // True if this edge lies on the triangle boundary.
    public bool IsBoundary { get; }

    public PslgEdge(int start, int end, bool isBoundary)
    {
        Start = start;
        End = end;
        IsBoundary = isBoundary;
    }
}

public struct HalfEdge
{
    public int From { get; set; }
    public int To { get; set; }
    public int Twin { get; set; }
    public int Next { get; set; }
    public bool IsBoundary { get; set; }
}

public sealed class PslgFace
{
    // CCW outer boundary vertex indices.
    public int[] OuterVertices { get; }

    // Zero or more CCW hole cycles.
    public IReadOnlyList<int[]> Holes { get; }

    // Signed area in UV: outer area minus sum(holes).
    public double SignedAreaUV { get; }

    // Backward compatibility helpers for older tests/usage.
    public int[] VertexIndices => OuterVertices;
    public double SignedArea => SignedAreaUV;

    public PslgFace(int[] outerVertices, double signedArea)
        : this(outerVertices, Array.Empty<int[]>(), signedArea)
    {
    }

    public PslgFace(int[] outerVertices, IReadOnlyList<int[]> holes, double signedArea)
    {
        OuterVertices = outerVertices;
        Holes = holes;
        SignedAreaUV = signedArea;
    }
}

public readonly struct PslgFaceSelection
{
    public int OuterFaceIndex { get; }
    public IReadOnlyList<PslgFace> InteriorFaces { get; }

    public PslgFaceSelection(int outerFaceIndex, IReadOnlyList<PslgFace> interiorFaces)
    {
        OuterFaceIndex = outerFaceIndex;
        InteriorFaces = interiorFaces;
    }
}

public static class PslgBuilder
{
    // Builds an initial PSLG for one triangle and its intersection points/segments.
    //
    // Vertices:
    //   - 0: corner V0 -> (1, 0)
    //   - 1: corner V1 -> (0, 1)
    //   - 2: corner V2 -> (0, 0)
    //   - 3..: intersection points in the same order as the input list,
    //          mapped to (u, v) from their barycentric coordinates.
    //
    // Edges:
    //   - Boundary edges:
    //       (0,1), (1,2), (2,0) marked IsBoundary = true.
    //   - Segment edges:
    //       For each IntersectionSegment (i,j), we add an edge between
    //       vertices (3 + i) and (3 + j) with IsBoundary = false.
    //
    // This is intentionally minimal and does not yet:
    //   - split boundary edges at intersection points,
    //   - enforce that edges do not pass through other vertices,
    //   - build half-edge or face structures.
    // Future phases will refine this representation.
    public static void Build(
        IReadOnlyList<TriangleSubdivision.IntersectionPoint> points,
        IReadOnlyList<TriangleSubdivision.IntersectionSegment> segments,
        out List<PslgVertex> vertices,
        out List<PslgEdge> edges)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (segments is null) throw new ArgumentNullException(nameof(segments));

        vertices = new List<PslgVertex>(capacity: 3 + points.Count);

        // Triangle corners in barycentric (u,v) chart.
        vertices.Add(new PslgVertex(1.0, 0.0, isTriangleCorner: true, cornerIndex: 0)); // V0
        vertices.Add(new PslgVertex(0.0, 1.0, isTriangleCorner: true, cornerIndex: 1)); // V1
        vertices.Add(new PslgVertex(0.0, 0.0, isTriangleCorner: true, cornerIndex: 2)); // V2

        // Intersection points.
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            double u = p.Barycentric.U;
            double v = p.Barycentric.V;
            vertices.Add(new PslgVertex(u, v, isTriangleCorner: false, cornerIndex: -1));
        }

        // Phase C3: normalize vertices (clamp, snap, deduplicate) and keep a
        // mapping from original indices to representatives.
        var indexMap = NormalizeVertices(vertices);

        // Phase D: build boundary edges, intersection edges, and verify that
        // there are no crossings without explicit vertices.
        edges = new List<PslgEdge>(capacity: 3 + segments.Count);
        var edgeKeys = new HashSet<(int, int)>();

        BuildBoundaryEdges(vertices, edges, edgeKeys);
        BuildIntersectionEdges(points, segments, indexMap, vertices, edges, edgeKeys);
        VerifyNoCrossings(vertices, edges);
    }

    // Phase C3: snap and deduplicate PSLG vertices.
    //
    // - Clamp (u,v) into the reference triangle domain:
    //       u >= 0, v >= 0, u + v <= 1.
    // - Snap vertices that are within EpsCorner of a triangle corner onto that
    //   corner exactly.
    // - Merge vertices whose distance is within EpsVertex, keeping a single
    //   representative. Returns an index map from original indices to
    //   representative vertex indices.
    private static int[] NormalizeVertices(List<PslgVertex> vertices)
    {
        if (vertices.Count == 0)
        {
            return Array.Empty<int>();
        }

        var original = vertices.ToArray();
        var newVertices = new List<PslgVertex>(original.Length);
        var indexMap = new int[original.Length];

        var corner0 = original[0];
        var corner1 = original[1];
        var corner2 = original[2];

        for (int i = 0; i < original.Length; i++)
        {
            var vOrig = original[i];
            double x = vOrig.X;
            double y = vOrig.Y;

            bool isCorner = vOrig.IsTriangleCorner;
            int cornerIndex = vOrig.CornerIndex;

            if (!isCorner)
            {
                (x, y) = ClampToReferenceTriangle(x, y);

                int snappedCorner = SnapToCorner(x, y, corner0, corner1, corner2);
                if (snappedCorner >= 0)
                {
                    isCorner = true;
                    cornerIndex = snappedCorner;

                    switch (snappedCorner)
                    {
                        case 0:
                            x = corner0.X;
                            y = corner0.Y;
                            break;
                        case 1:
                            x = corner1.X;
                            y = corner1.Y;
                            break;
                        case 2:
                            x = corner2.X;
                            y = corner2.Y;
                            break;
                    }
                }
            }

            int representativeIndex = -1;
            for (int j = 0; j < newVertices.Count; j++)
            {
                var existing = newVertices[j];
                double dx = x - existing.X;
                double dy = y - existing.Y;
                double dist2 = dx * dx + dy * dy;

                if (dist2 <= Tolerances.PslgVertexMergeEpsilonSquared)
                {
                    representativeIndex = j;
                    break;
                }
            }

            if (representativeIndex < 0)
            {
                newVertices.Add(new PslgVertex(x, y, isCorner, cornerIndex));
                representativeIndex = newVertices.Count - 1;
            }

            indexMap[i] = representativeIndex;
        }

        vertices.Clear();
        vertices.AddRange(newVertices);
        return indexMap;
    }

    private static (double x, double y) ClampToReferenceTriangle(double u, double v)
    {
        if (u < 0.0) u = 0.0;
        if (v < 0.0) v = 0.0;

        if (u > 1.0) u = 1.0;
        if (v > 1.0) v = 1.0;

        double sum = u + v;
        if (sum > 1.0)
        {
            double inv = 1.0 / sum;
            u *= inv;
            v *= inv;
        }

        return (u, v);
    }

    private static int SnapToCorner(
        double x,
        double y,
        PslgVertex corner0,
        PslgVertex corner1,
        PslgVertex corner2)
    {
        int bestIndex = -1;
        double bestDist2 = Tolerances.EpsCorner * Tolerances.EpsCorner;

        void Consider(int index, PslgVertex c)
        {
            double dx = x - c.X;
            double dy = y - c.Y;
            double dist2 = dx * dx + dy * dy;
            if (dist2 <= bestDist2)
            {
                bestDist2 = dist2;
                bestIndex = index;
            }
        }

        Consider(0, corner0);
        Consider(1, corner1);
        Consider(2, corner2);

        return bestIndex;
    }

    // Phase D1: triangle boundary edges, split at all vertices that lie on
    // each side. Edges are oriented along the triangle boundary cycle
    // V0->V1->V2->V0.
    private static void BuildBoundaryEdges(
        List<PslgVertex> vertices,
        List<PslgEdge> edges,
        HashSet<(int, int)> edgeKeys)
    {
        // Side 0: V0 -> V1, with vertices satisfying u + v = 1 (within eps).
        BuildBoundarySide(
            vertices,
            edges,
            edgeKeys,
            static v => Math.Abs(v.X + v.Y - 1.0) <= Tolerances.EpsSide,
            static v => v.Y,
            ascending: true);

        // Side 1: V1 -> V2, with vertices satisfying u = 0.
        BuildBoundarySide(
            vertices,
            edges,
            edgeKeys,
            static v => Math.Abs(v.X) <= Tolerances.EpsSide,
            static v => v.Y,
            ascending: false);

        // Side 2: V2 -> V0, with vertices satisfying v = 0.
        BuildBoundarySide(
            vertices,
            edges,
            edgeKeys,
            static v => Math.Abs(v.Y) <= Tolerances.EpsSide,
            static v => v.X,
            ascending: true);
    }

    private static void BuildBoundarySide(
        List<PslgVertex> vertices,
        List<PslgEdge> edges,
        HashSet<(int, int)> edgeKeys,
        Func<PslgVertex, bool> isOnSide,
        Func<PslgVertex, double> param,
        bool ascending)
    {
        var indices = new List<(int index, double t)>();

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            if (isOnSide(v))
            {
                indices.Add((i, param(v)));
            }
        }

        if (indices.Count < 2)
        {
            return;
        }

        indices.Sort((a, b) => a.t.CompareTo(b.t));
        if (!ascending)
        {
            indices.Reverse();
        }

        for (int i = 0; i < indices.Count - 1; i++)
        {
            int a = indices[i].index;
            int b = indices[i + 1].index;
            if (a == b)
            {
                continue;
            }

            var key = a < b ? (a, b) : (b, a);
            if (edgeKeys.Add(key))
            {
                edges.Add(new PslgEdge(a, b, isBoundary: true));
            }
        }
    }

    // Phase D2: intersection edges between PSLG vertices corresponding to
    // IntersectionSegment endpoints (after vertex deduplication).
    private static void BuildIntersectionEdges(
        IReadOnlyList<TriangleSubdivision.IntersectionPoint> points,
        IReadOnlyList<TriangleSubdivision.IntersectionSegment> segments,
        int[] indexMap,
        List<PslgVertex> vertices,
        List<PslgEdge> edges,
        HashSet<(int, int)> edgeKeys)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.StartIndex < 0 || seg.StartIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment start index is out of range.");
            }

            if (seg.EndIndex < 0 || seg.EndIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment end index is out of range.");
            }

            int startOriginal = 3 + seg.StartIndex;
            int endOriginal = 3 + seg.EndIndex;

            if ((uint)startOriginal >= (uint)indexMap.Length ||
                (uint)endOriginal >= (uint)indexMap.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment indices are out of range after vertex normalization.");
            }

            int start = indexMap[startOriginal];
            int end = indexMap[endOriginal];

            if (start == end)
            {
                continue;
            }

            var key = start < end ? (start, end) : (end, start);
            if (edgeKeys.Add(key))
            {
                edges.Add(new PslgEdge(start, end, isBoundary: false));
            }
        }
    }

    // Phase D3: verify that no two edges cross in their interiors without
    // an explicit PSLG vertex at the crossing.
    private static void VerifyNoCrossings(
        List<PslgVertex> vertices,
        List<PslgEdge> edges)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var e1 = edges[i];
            for (int j = i + 1; j < edges.Count; j++)
            {
                var e2 = edges[j];

                // Ignore edges that share a vertex: junctions and endpoints
                // are allowed; only pure crossings are forbidden.
                if (e1.Start == e2.Start || e1.Start == e2.End ||
                    e1.End == e2.Start || e1.End == e2.End)
                {
                    continue;
                }

                if (!RealSegmentPredicates.TryIntersect(
                        new RealSegment(
                            new RealPoint(vertices[e1.Start].X, vertices[e1.Start].Y, 0.0),
                            new RealPoint(vertices[e1.End].X, vertices[e1.End].Y, 0.0)),
                        new RealSegment(
                            new RealPoint(vertices[e2.Start].X, vertices[e2.Start].Y, 0.0),
                            new RealPoint(vertices[e2.End].X, vertices[e2.End].Y, 0.0)),
                        out var intersection))
                {
                    continue;
                }

                if (!IsNearExistingVertex(vertices, intersection.X, intersection.Y))
                {
                    throw new InvalidOperationException("PSLG requires no crossings without vertices.");
                }
            }
        }
    }

    private static bool IsNearExistingVertex(
        List<PslgVertex> vertices,
        double x,
        double y)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            double dx = x - v.X;
            double dy = y - v.Y;
            double dist2 = dx * dx + dy * dy;
            if (dist2 <= Tolerances.PslgVertexMergeEpsilonSquared)
            {
                return true;
            }
        }

        return false;
    }

    // Phase E1: build half-edges with twin links; Phase E2: assign Next using
    // angular ordering of outgoing half-edges per vertex.
    public static void BuildHalfEdges(
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        out List<HalfEdge> halfEdges)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (edges is null) throw new ArgumentNullException(nameof(edges));

        halfEdges = new List<HalfEdge>(edges.Count * 2);

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            int idx = halfEdges.Count;
            halfEdges.Add(new HalfEdge
            {
                From = e.Start,
                To = e.End,
                Twin = idx + 1,
                Next = -1,
                IsBoundary = e.IsBoundary
            });

            halfEdges.Add(new HalfEdge
            {
                From = e.End,
                To = e.Start,
                Twin = idx,
                Next = -1,
                IsBoundary = e.IsBoundary
            });
        }

        var angles = new double[halfEdges.Count];
        var outgoing = new List<(int edgeIndex, double angle)>[vertices.Count];

        for (int i = 0; i < halfEdges.Count; i++)
        {
            var he = halfEdges[i];
            var from = vertices[he.From];
            var to = vertices[he.To];
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            angles[i] = Math.Atan2(dy, dx);

            outgoing[he.From] ??= new List<(int edgeIndex, double angle)>();
            outgoing[he.From].Add((i, angles[i]));
        }

        for (int v = 0; v < outgoing.Length; v++)
        {
            var list = outgoing[v];
            if (list is null || list.Count == 0)
            {
                continue;
            }

            list.Sort((a, b) => a.angle.CompareTo(b.angle));
        }

        for (int i = 0; i < halfEdges.Count; i++)
        {
            var he = halfEdges[i];
            int toVertex = he.To;
            var list = outgoing[toVertex];
            if (list is null || list.Count == 0)
            {
                throw new InvalidOperationException("Half-edge has no outgoing edges at its destination vertex.");
            }

            int twin = he.Twin;
            int idxInList = -1;
            for (int k = 0; k < list.Count; k++)
            {
                if (list[k].edgeIndex == twin)
                {
                    idxInList = k;
                    break;
                }
            }

            if (idxInList < 0)
            {
                throw new InvalidOperationException("Twin half-edge not found in outgoing list.");
            }

            // Next edge follows the left face: CCW successor of the twin at the destination vertex.
            int nextIdx = (idxInList + 1) % list.Count;
            int nextEdge = list[nextIdx].edgeIndex;

            var temp = halfEdges[i];
            temp.Next = nextEdge;
            halfEdges[i] = temp;
        }
    }

    // Phase E3: walk faces using half-edge Next pointers. Every half-edge
    // belongs to exactly one directed face cycle.
    public static List<PslgFace> ExtractFaces(
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<HalfEdge> halfEdges)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (halfEdges is null) throw new ArgumentNullException(nameof(halfEdges));

        var rawCycles = new List<RawCycle>();
        var visited = new bool[halfEdges.Count];

        for (int i = 0; i < halfEdges.Count; i++)
        {
            if (visited[i])
            {
                continue;
            }

            var cycle = new List<int>();
            int start = i;
            int current = start;

            for (int step = 0; step <= halfEdges.Count; step++)
            {
                if (visited[current])
                {
                    if (current == start)
                    {
                        break;
                    }

                    throw new InvalidOperationException("Half-edge cycle did not close to its starting edge.");
                }

                visited[current] = true;
                var he = halfEdges[current];
                cycle.Add(he.From);

                if (he.Next < 0 || he.Next >= halfEdges.Count)
                {
                    throw new InvalidOperationException("Half-edge Next pointer is out of range.");
                }

                current = he.Next;
                if (current == start)
                {
                    break;
                }
            }

            if (current != start)
            {
                throw new InvalidOperationException("Half-edge traversal exceeded the number of half-edges without closing a cycle.");
            }

            if (cycle.Count >= 3)
            {
                var polyPoints = new List<RealPoint>(cycle.Count);
                double cx = 0.0, cy = 0.0;
                foreach (var vi in cycle)
                {
                    var v = vertices[vi];
                    polyPoints.Add(new RealPoint(v.X, v.Y, 0.0));
                    cx += v.X;
                    cy += v.Y;
                }

                double area = new RealPolygon(polyPoints).SignedArea;
                double inv = 1.0 / cycle.Count;
                var sample = (X: cx * inv, Y: cy * inv);
                rawCycles.Add(new RawCycle(cycle.ToArray(), area, sample));
            }
        }

        return BuildFacesWithHoles(rawCycles, vertices);
    }

    // Phase F: select bounded interior faces by removing the outer face
    // (largest absolute area) and any vanishing sliver faces.
    public static List<PslgFace> SelectInteriorFaces(
        IReadOnlyList<PslgFace> faces)
    {
        if (faces is null) throw new ArgumentNullException(nameof(faces));
        if (faces.Count == 0) return new List<PslgFace>();

        var filtered = new List<PslgFace>(faces.Count);
        for (int i = 0; i < faces.Count; i++)
        {
            double areaAbs = Math.Abs(faces[i].SignedAreaUV);
            if (areaAbs <= Tolerances.EpsArea)
            {
                continue;
            }
            filtered.Add(faces[i]);
        }

        return DeduplicateFaces(filtered);
    }

    internal static PslgFaceSelection SelectInteriorFaces(
        IReadOnlyList<PslgFace> faces,
        double expectedTriangleArea,
        double epsAbsolute = Tolerances.EpsArea,
        double epsRelative = Tolerances.BarycentricInsideEpsilon)
    {
        if (faces is null) throw new ArgumentNullException(nameof(faces));
        if (faces.Count == 0) return new PslgFaceSelection(-1, Array.Empty<PslgFace>());

        int outerIndex = 0;
        double outerAbs = Math.Abs(faces[0].SignedAreaUV);
        for (int i = 1; i < faces.Count; i++)
        {
            double abs = Math.Abs(faces[i].SignedAreaUV);
            if (abs > outerAbs)
            {
                outerAbs = abs;
                outerIndex = i;
            }
        }

        var interiors = new List<PslgFace>(faces.Count - 1);
        var seenOuterKey = CanonicalFaceKey(faces[outerIndex].OuterVertices);

        for (int i = 0; i < faces.Count; i++)
        {
            if (i == outerIndex)
            {
                continue;
            }

            double areaAbs = Math.Abs(faces[i].SignedAreaUV);
            if (areaAbs <= Tolerances.EpsArea)
            {
                continue;
            }

            // Skip any duplicate of the outer boundary face.
            var key = CanonicalFaceKey(faces[i].OuterVertices);
            if (key == seenOuterKey)
            {
                continue;
            }

            interiors.Add(faces[i]);
        }

        interiors = DeduplicateFaces(interiors);

        double sumInterior = 0.0;
        for (int i = 0; i < interiors.Count; i++)
        {
            sumInterior += interiors[i].SignedAreaUV;
        }

        if (!double.IsNaN(expectedTriangleArea))
        {
            double diffSigned = Math.Abs(sumInterior - expectedTriangleArea);
            double diffAbs = Math.Abs(Math.Abs(sumInterior) - Math.Abs(expectedTriangleArea));
            double rel = epsRelative * Math.Abs(expectedTriangleArea);
            bool withinSigned = diffSigned <= epsAbsolute || diffSigned <= rel;
            bool withinAbs = diffAbs <= epsAbsolute || diffAbs <= rel;

            if (!withinSigned && !withinAbs)
            {
                throw new InvalidOperationException(
                    $"PSLG interior face area check failed: expected={expectedTriangleArea}, sumInterior={sumInterior}, " +
                    $"outerIndex={outerIndex}, faces={faces.Count}, interiors={interiors.Count}");
            }
        }

        return new PslgFaceSelection(outerIndex, interiors);
    }

    private readonly struct RawCycle
    {
        public int[] Vertices { get; }
        public double Area { get; }
        public (double X, double Y) Sample { get; }

        public RawCycle(int[] vertices, double area, (double X, double Y) sample)
        {
            Vertices = vertices;
            Area = area;
            Sample = sample;
        }
    }

    private static List<PslgFace> BuildFacesWithHoles(
        IReadOnlyList<RawCycle> cycles,
        IReadOnlyList<PslgVertex> vertices)
    {
        if (cycles.Count == 0) return new List<PslgFace>();

        // Normalize orientation to CCW and positive area.
        var norm = new List<RawCycle>(cycles.Count);
        foreach (var c in cycles)
        {
            var verts = c.Vertices.ToArray();
            double area = c.Area;
            if (area < 0)
            {
                Array.Reverse(verts);
                area = -area;
            }
            norm.Add(new RawCycle(verts, area, c.Sample));
        }

        int n = norm.Count;
        var parent = Enumerable.Repeat(-1, n).ToArray();
        var depth = new int[n];

        // Point-in-polygon helper.
        bool Contains(RawCycle outer, (double X, double Y) p)
        {
            var pts = new List<RealPoint>(outer.Vertices.Length);
            foreach (var vi in outer.Vertices)
            {
                var v = vertices[vi];
                pts.Add(new RealPoint(v.X, v.Y, 0.0));
            }
            return RealPolygonPredicates.ContainsInclusive(new RealPolygon(pts), new RealPoint(p.X, p.Y, 0.0));
        }

        // Assign parent: smallest-area cycle that strictly contains the sample.
        for (int i = 0; i < n; i++)
        {
            double bestArea = double.MaxValue;
            int best = -1;
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var outer = norm[j];
                if (outer.Area <= norm[i].Area) continue;
                if (!Contains(outer, norm[i].Sample)) continue;
                if (outer.Area < bestArea)
                {
                    bestArea = outer.Area;
                    best = j;
                }
            }
            parent[i] = best;
            if (best >= 0)
            {
                depth[i] = depth[best] + 1;
            }
        }

        var children = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            int p = parent[i];
            if (p >= 0)
            {
                children[p] ??= new List<int>();
                children[p].Add(i);
            }
        }

        var faces = new List<PslgFace>();
        for (int i = 0; i < n; i++)
        {
            var holes = new List<int[]>();
            var holeKeys = new HashSet<string>();
            if (children[i] != null)
            {
                foreach (var ch in children[i])
                {
                    if (depth[ch] == depth[i] + 1)
                    {
                        var key = CanonicalFaceKey(norm[ch].Vertices);
                        if (holeKeys.Add(key))
                        {
                            holes.Add(norm[ch].Vertices);
                        }
                    }
                }
            }

            double holeAreaSum = 0.0;
            foreach (var h in holes)
            {
                holeAreaSum += CycleArea(vertices, h);
            }

            double signedArea = norm[i].Area - holeAreaSum;
            if (Math.Abs(signedArea) <= Tolerances.EpsArea)
            {
                continue;
            }

            faces.Add(new PslgFace(norm[i].Vertices, holes, signedArea));
        }

        return DeduplicateFaces(faces);
    }

    private static double CycleArea(IReadOnlyList<PslgVertex> vertices, int[] cycle)
    {
        var pts = new List<RealPoint>(cycle.Length);
        foreach (var vi in cycle)
        {
            var v = vertices[vi];
            pts.Add(new RealPoint(v.X, v.Y, 0.0));
        }
        double area = new RealPolygon(pts).SignedArea;
        return area < 0 ? -area : area;
    }

    private static List<PslgFace> DeduplicateFaces(IReadOnlyList<PslgFace> faces)
    {
        var unique = new List<PslgFace>(faces.Count);
        var seen = new HashSet<string>();

        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            var key = CanonicalFaceKey(face.OuterVertices);
            if (seen.Add(key))
            {
                unique.Add(face);
            }
        }

        return unique;
    }

    private static string CanonicalFaceKey(int[] vertices)
    {
        if (vertices is null || vertices.Length == 0)
        {
            return string.Empty;
        }

        int n = vertices.Length;
        int bestStart = 0;

        for (int start = 1; start < n; start++)
        {
            bool better = false;
            for (int k = 0; k < n; k++)
            {
                int a = vertices[(start + k) % n];
                int b = vertices[(bestStart + k) % n];
                if (a == b)
                {
                    continue;
                }

                if (a < b)
                {
                    better = true;
                }

                break;
            }

            if (better)
            {
                bestStart = start;
            }
        }

        var ordered = new int[n];
        for (int i = 0; i < n; i++)
        {
            ordered[i] = vertices[(bestStart + i) % n];
        }

        return string.Join(",", ordered);
    }

    // Phase G: ear-clipping triangulation of one simple polygonal face in
    // parameter space. Supports optional holes by bridging them into a
    // single simple polygon before ear clipping. Returns triangles as
    // triples of vertex indices into the global PSLG vertex list.
    internal static List<(int A, int B, int C)> TriangulateFace(
        PslgFace face,
        IReadOnlyList<PslgVertex> vertices)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (face.OuterVertices is null || face.OuterVertices.Length < 3)
        {
            throw new ArgumentException("Face must have an outer boundary with at least 3 vertices.", nameof(face));
        }

        if (face.Holes.Count == 0)
        {
            return TriangulateSimple(face.OuterVertices, vertices, face.SignedAreaUV);
        }

        return TriangulateWithHoles(face, vertices);
    }

    private static List<(int A, int B, int C)> TriangulateSimple(
        int[] polygon,
        IReadOnlyList<PslgVertex> vertices,
        double expectedArea)
    {
        var polyList = new List<int>(polygon);
        if (polyList.Count < 3)
        {
            throw new InvalidOperationException("Face must have at least 3 vertices.");
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

    private static List<(int A, int B, int C)> TriangulateWithHoles(
        PslgFace face,
        IReadOnlyList<PslgVertex> vertices)
    {
        // Build visibility-tested bridges and stitch into a simple polygon.
        var stitched = StitchHoles(face, vertices);
        return TriangulateSimple(stitched.ToArray(), vertices, face.SignedAreaUV);
    }

    private static List<int> StitchHoles(PslgFace face, IReadOnlyList<PslgVertex> vertices)
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
        foreach (var hole in face.Holes)
        {
            AddCycleSegments(hole);
        }

        var uniqueHoles = new List<int[]>(face.Holes.Count);
        var holeKeys = new HashSet<string>();
        foreach (var hcyc in face.Holes)
        {
            var key = CanonicalFaceKey(hcyc);
            if (holeKeys.Add(key))
            {
                uniqueHoles.Add(hcyc);
            }
        }

        foreach (var hole in uniqueHoles)
        {
            if (hole.Length < 3) continue;

            // Pick hole vertex with smallest (x,y).
            int hIndex = 0;
            for (int i = 1; i < hole.Length; i++)
            {
                var vh = vertices[hole[i]];
                var vbest = vertices[hole[hIndex]];
                if (vh.X < vbest.X - Tolerances.EpsVertex ||
                    (Math.Abs(vh.X - vbest.X) <= Tolerances.EpsVertex && vh.Y < vbest.Y))
                {
                    hIndex = i;
                }
            }
            int hVertex = hole[hIndex];

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
                throw new InvalidOperationException("Failed to find a visible bridge from hole to outer boundary.");
            }

            // Build stitched path:
            // outer[0..bestOuterIdx], bridge to h, traverse hole CW from h back to h,
            // bridge back to outer[bestOuterIdx], then continue outer.
            var stitched = new List<int>(polygon.Count + hole.Length + 3);
            for (int i = 0; i <= bestOuterIdx; i++)
            {
                stitched.Add(polygon[i]);
            }

            stitched.Add(hVertex); // enter hole

            for (int k = 1; k < hole.Length; k++)
            {
                int idx = (hIndex - k + hole.Length) % hole.Length; // CW order
                stitched.Add(hole[idx]);
            }

            stitched.Add(hVertex); // exit hole
            stitched.Add(polygon[bestOuterIdx]); // bridge back to outer

            for (int i = bestOuterIdx + 1; i < polygon.Count; i++)
            {
                stitched.Add(polygon[i]);
            }

            polygon = stitched;

            // Rebuild segments with the new polygon cycle (outer edges plus hole perimeter and bridges).
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
    internal static IReadOnlyList<RealTriangle> TriangulateInteriorFaces(
        Triangle triangle,
        IReadOnlyList<PslgVertex> vertices,
        PslgFaceSelection selection)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (selection.InteriorFaces is null) throw new ArgumentNullException(nameof(selection));

        var patches = new List<RealTriangle>();

        RealPoint MapVertex(int idx)
        {
            double u = vertices[idx].X;
            double v = vertices[idx].Y;
            double w = 1.0 - u - v;
            var bary = new Barycentric(u, v, w);
            return triangle.FromBarycentric(in bary);
        }

        foreach (var face in selection.InteriorFaces)
        {
            var tris = TriangulateFace(face, vertices);

            double uvTriSum = 0.0;
            foreach (var t in tris)
            {
                double areaUv = new RealTriangle(
                    new RealPoint(vertices[t.A].X, vertices[t.A].Y, 0.0),
                    new RealPoint(vertices[t.B].X, vertices[t.B].Y, 0.0),
                    new RealPoint(vertices[t.C].X, vertices[t.C].Y, 0.0)).SignedArea;
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

                double area = TriangleArea3D(p0, p1, p2);
                if (area <= 0)
                {
                    throw new InvalidOperationException("Mapped triangle has non-positive area in world space.");
                }

                patches.Add(new RealTriangle(p0, p1, p2));
            }
        }

        return patches;
    }

    private static double TriangleArea3D(RealPoint a, RealPoint b, RealPoint c)
    {
        double abx = b.X - a.X;
        double aby = b.Y - a.Y;
        double abz = b.Z - a.Z;

        double acx = c.X - a.X;
        double acy = c.Y - a.Y;
        double acz = c.Z - a.Z;

        double cxp = aby * acz - abz * acy;
        double cyp = abz * acx - abx * acz;
        double czp = abx * acy - aby * acx;

        double len = Math.Sqrt(cxp * cxp + cyp * cyp + czp * czp);
        return 0.5 * len;
    }

}
