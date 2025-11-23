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

public readonly struct PslgFace
{
    public int[] VertexIndices { get; }
    public double SignedArea { get; }

    public PslgFace(int[] vertexIndices, double signedArea)
    {
        VertexIndices = vertexIndices;
        SignedArea = signedArea;
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
        var outgoing = new List<int>[vertices.Count];

        for (int i = 0; i < halfEdges.Count; i++)
        {
            var he = halfEdges[i];
            var from = vertices[he.From];
            var to = vertices[he.To];
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            angles[i] = Math.Atan2(dy, dx);

            outgoing[he.From] ??= new List<int>();
            outgoing[he.From].Add(i);
        }

        const double fullTurn = 2 * Math.PI;

        for (int v = 0; v < outgoing.Length; v++)
        {
            var list = outgoing[v];
            if (list is null || list.Count == 0)
            {
                continue;
            }

            list.Sort((a, b) => angles[a].CompareTo(angles[b]));
        }

        for (int i = 0; i < halfEdges.Count; i++)
        {
            var he = halfEdges[i];
            int atVertex = he.To;
            var list = outgoing[atVertex];
            if (list is null || list.Count == 0)
            {
                continue;
            }

            double incomingAngle = Math.Atan2(
                vertices[he.From].Y - vertices[atVertex].Y,
                vertices[he.From].X - vertices[atVertex].X);

            int chosen = list[0];
            double bestDelta = double.MaxValue;

            for (int k = 0; k < list.Count; k++)
            {
                int candidate = list[k];
                double delta = angles[candidate] - incomingAngle;
                while (delta <= 0)
                {
                    delta += fullTurn;
                }

                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    chosen = candidate;
                }
            }

            var temp = halfEdges[i];
            temp.Next = chosen;
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

        var faces = new List<PslgFace>();
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
        foreach (var vi in cycle)
        {
            var v = vertices[vi];
            polyPoints.Add(new RealPoint(v.X, v.Y, 0.0));
        }

        double area = new RealPolygon(polyPoints).SignedArea;
                faces.Add(new PslgFace(cycle.ToArray(), area));
            }
        }

        return faces;
    }

    // Phase F: select bounded interior faces by removing the outer face
    // (largest absolute area) and any vanishing sliver faces.
    public static List<PslgFace> SelectInteriorFaces(
        IReadOnlyList<PslgFace> faces)
    {
        // Classification-only wrapper: no area invariant enforced.
        return SelectInteriorFaces(faces, double.NaN).InteriorFaces.ToList();
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
        double outerAbs = Math.Abs(faces[0].SignedArea);
        for (int i = 1; i < faces.Count; i++)
        {
            double abs = Math.Abs(faces[i].SignedArea);
            if (abs > outerAbs)
            {
                outerAbs = abs;
                outerIndex = i;
            }
        }

        var interiors = new List<PslgFace>(faces.Count - 1);
        double sumInterior = 0.0;

        for (int i = 0; i < faces.Count; i++)
        {
            if (i == outerIndex)
            {
                continue;
            }

            double areaAbs = Math.Abs(faces[i].SignedArea);
            if (areaAbs <= Tolerances.EpsArea)
            {
                continue;
            }

            interiors.Add(faces[i]);
            sumInterior += faces[i].SignedArea;
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

    // Phase G: ear-clipping triangulation of one simple polygonal face in
    // parameter space. Returns triangles as triples of vertex indices into
    // the global PSLG vertex list.
    internal static List<(int A, int B, int C)> TriangulateFace(
        PslgFace face,
        IReadOnlyList<PslgVertex> vertices)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (face.VertexIndices is null || face.VertexIndices.Length < 3)
        {
            throw new ArgumentException("Face must have at least 3 vertices.", nameof(face));
        }

        var polygon = new List<int>(face.VertexIndices);
        if (polygon.Count < 3)
        {
            throw new InvalidOperationException("Face must have at least 3 vertices.");
        }

        double targetSignedArea = face.SignedArea;
        int orient = targetSignedArea < 0 ? -1 : 1;
        if (orient == -1)
        {
            polygon.Reverse();
            targetSignedArea = -targetSignedArea;
        }

        var triangles = new List<(int A, int B, int C)>(polygon.Count - 2);

        var faceCoords = new List<RealPoint>(polygon.Count);
        foreach (var idx in polygon)
        {
            faceCoords.Add(new RealPoint(vertices[idx].X, vertices[idx].Y, 0.0));
        }


        while (polygon.Count > 3)
        {
            bool earFound = false;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                int prev = polygon[(i - 1 + n) % n];
                int curr = polygon[i];
                int next = polygon[(i + 1) % n];

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
                            new RealPoint(vertices[polygon[k]].X, vertices[polygon[k]].Y, 0.0)))
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
                polygon.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                throw new InvalidOperationException("Ear clipping failed: no valid ear found for a non-triangular polygon.");
            }
        }

        triangles.Add((polygon[0], polygon[1], polygon[2]));

        if (triangles.Count != face.VertexIndices.Length - 2)
        {
            throw new InvalidOperationException("Ear clipping produced an unexpected triangle count.");
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

        double signedSum = orient * sumArea;
        double absExpected = Math.Abs(face.SignedArea);
        double diffAbs = Math.Abs(sumArea - absExpected);
        double diffSigned = Math.Abs(signedSum - face.SignedArea);
        double rel = Tolerances.BarycentricInsideEpsilon * absExpected;
        if (diffAbs > Tolerances.EpsArea && diffAbs > rel && diffSigned > Tolerances.EpsArea && diffSigned > rel)
        {
            throw new InvalidOperationException("Ear clipping area check failed for face.");
        }

        return triangles;
    }

    internal static IReadOnlyList<RealTriangle> TriangulateInteriorFaces(
        Triangle triangle,
        IReadOnlyList<PslgVertex> vertices,
        PslgFaceSelection selection)
    {
        if (vertices is null) throw new ArgumentNullException(nameof(vertices));
        if (selection.InteriorFaces is null) throw new ArgumentNullException(nameof(selection));

        var patches = new List<RealTriangle>();
        double totalArea = 0.0;

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
            foreach (var t in tris)
            {
                var p0 = MapVertex(t.A);
                var p1 = MapVertex(t.B);
                var p2 = MapVertex(t.C);

                double area = new RealTriangle(p0, p1, p2).SignedArea * 1.0; // SignedArea in XY; use magnitude below
                area = Math.Abs(area);
                if (area <= 0)
                {
                    throw new InvalidOperationException("Mapped triangle has non-positive area in world space.");
                }

                totalArea += area;
                patches.Add(new RealTriangle(p0, p1, p2));
            }
        }

        double triArea = Math.Abs(new RealTriangle(
            new RealPoint(triangle.P0),
            new RealPoint(triangle.P1),
            new RealPoint(triangle.P2)).SignedArea);

        double diff = Math.Abs(totalArea - triArea);
        double relTol = Tolerances.BarycentricInsideEpsilon * triArea;
        if (diff > Tolerances.EpsArea && diff > relTol)
        {
            throw new InvalidOperationException(
                $"Total patch area mismatch: expected {triArea}, got {totalArea}");
        }

        return patches;
    }

}
