using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

// Represents the global intersection graph between two meshes.
//
// For a given IntersectionSet this builds:
//   - Pair-local feature boxes (PairFeatures) for each intersecting triangle pair,
//   - A deduplicated set of global intersection vertices in world space,
//   - A deduplicated set of undirected edges between those vertices.
//
// Higher-level boolean logic will eventually operate on this graph
// rather than directly on the raw IntersectionSet.
public sealed class IntersectionGraph
{
    public IntersectionSet IntersectionSet { get; }
    public IReadOnlyList<(IntersectionVertexId Id, RealPoint Position)> Vertices { get; }
    public IReadOnlyList<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)> Edges { get; }

    // Per-intersection feature boxes, one for each entry in
    // IntersectionSet.Intersections, in matching index order.
    public IReadOnlyList<PairFeatures> Pairs { get; }

    public IReadOnlyDictionary<(int PairIndex, int LocalVertexId), IntersectionVertexId> PairVertexIds { get; private init; } = new Dictionary<(int, int), IntersectionVertexId>();

    internal IntersectionGraph(IntersectionSet intersectionSet,
                               IReadOnlyList<(IntersectionVertexId, RealPoint)> vertices,
                               IReadOnlyList<(IntersectionEdgeId, IntersectionVertexId, IntersectionVertexId)> edges,
                               IReadOnlyList<PairFeatures> pairs)
    {
        if (intersectionSet.TrianglesA is null)
            throw new ArgumentNullException(nameof(intersectionSet));

        IntersectionSet = intersectionSet;
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        Pairs = pairs ?? throw new ArgumentNullException(nameof(pairs));
    }

    // Creates an IntersectionGraph from an existing IntersectionSet.
    //
    // This builds:
    //   - one local PairFeatures box per intersecting triangle pair, using
    //     the raw per-pair intersection geometry from PairIntersectionMath,
    //   - a global list of intersection vertices deduplicated in world space,
    //   - a global list of undirected edges between those vertices, deduped
    //     across all pairs.
    //
    // Higher-level boolean logic will eventually operate on this graph instead
    // of the raw IntersectionSet.
    public static IntersectionGraph FromIntersectionSet(IntersectionSet intersectionSet)
    {
        if (intersectionSet.TrianglesA is null)
            throw new ArgumentNullException(nameof(intersectionSet));

        var intersections = intersectionSet.Intersections;
        var pairs = new PairFeatures[intersections.Count];

        for (int i = 0; i < intersections.Count; i++)
        {
            var intersection = intersections[i];
            pairs[i] = PairFeaturesFactory.Create(in intersectionSet, in intersection);
        }

        // Global vertex construction.
        // For each local PairVertex, compute its world-space position using
        // barycentric coordinates on triangle A, then weld across neighboring
        // spatial cells so shared endpoints across pairs collapse
        // to a single global IntersectionVertexId.
        var globalVertices = new List<(IntersectionVertexId Id, RealPoint Position)>();
        var positions = new List<RealPoint>();
        var voxels = new Dictionary<(long X, long Y, long Z), List<int>>();
        var pairVertexToGlobal = new Dictionary<(int PairIndex, int LocalVertexId), IntersectionVertexId>();

        var trianglesA = intersectionSet.TrianglesA;

        for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
        {
            var pair = pairs[pairIndex];
            var intersection = pair.Intersection;
            var triangleA = trianglesA[intersection.TriangleIndexA];

            var localVertices = pair.Vertices;
            for (int i = 0; i < localVertices.Count; i++)
            {
                var v = localVertices[i];
                var barycentricOnA = v.OnTriangleA;
                var world = Barycentric.ToRealPointOnTriangle(in triangleA, in barycentricOnA);

                int id = VertexWeld.GetOrAddCanonicalId(world, Tolerances.MergeEpsilon, positions, voxels);
                var globalId = new IntersectionVertexId(id);
                if (id == globalVertices.Count) globalVertices.Add((globalId, world));

                pairVertexToGlobal[(pairIndex, v.VertexId.Value)] = globalId;
            }
        }

        // Global edge construction.
        // For each local PairSegment, map its endpoints to global vertices,
        // discard degenerate segments, then normalize as undirected edges and
        // deduplicate across the whole graph.
        var globalEdges = new List<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)>();
        var edgeLookup = new Dictionary<(int, int), IntersectionEdgeId>();

        for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
        {
            var pair = pairs[pairIndex];
            var segments = pair.Segments;

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                if (!pairVertexToGlobal.TryGetValue((pairIndex, segment.Start.VertexId.Value), out var startId) ||
                    !pairVertexToGlobal.TryGetValue((pairIndex, segment.End.VertexId.Value), out var endId))
                {
                    continue;
                }

                if (startId.Value == endId.Value)
                {
                    continue; // Degenerate segment.
                }

                int a = startId.Value;
                int b = endId.Value;
                if (b < a)
                {
                    (a, b) = (b, a);
                }

                var key = (a, b);
                if (!edgeLookup.TryGetValue(key, out var edgeId))
                {
                    edgeId = new IntersectionEdgeId(globalEdges.Count);
                    edgeLookup.Add(key, edgeId);
                    globalEdges.Add((edgeId, new IntersectionVertexId(a), new IntersectionVertexId(b)));
                }
            }
        }

        var splitEdges = SplitEdgesPassingThroughVertices(globalVertices, globalEdges);
        return new IntersectionGraph(intersectionSet, globalVertices, splitEdges, pairs) { PairVertexIds = pairVertexToGlobal };
    }

    private static IReadOnlyList<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)> SplitEdgesPassingThroughVertices(
        IReadOnlyList<(IntersectionVertexId Id, RealPoint Position)> vertices,
        IReadOnlyList<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)> edges)
    {
        if (edges.Count == 0 || vertices.Count < 3)
        {
            return edges;
        }

        double tEps = Tolerances.FeatureBarycentricEpsilon;
        double distanceEpsilon = 10.0 * Tolerances.MergeEpsilon;
        double distanceEpsilonSquared = distanceEpsilon * distanceEpsilon;

        var output = new List<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)>(edges.Count);
        var edgeIdByEndpoints = new Dictionary<(int, int), IntersectionEdgeId>(edges.Count);
        var interior = new List<(double T, int VertexValue)>();

        void AddEdgeDedup(int a, int b)
        {
            if (a == b)
            {
                return;
            }

            if (b < a)
            {
                (a, b) = (b, a);
            }

            var key = (a, b);
            if (edgeIdByEndpoints.ContainsKey(key))
            {
                return;
            }

            var id = new IntersectionEdgeId(output.Count);
            edgeIdByEndpoints.Add(key, id);
            output.Add((id, new IntersectionVertexId(a), new IntersectionVertexId(b)));
        }

        for (int edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
        {
            var edge = edges[edgeIndex];
            int startValue = edge.Start.Value;
            int endValue = edge.End.Value;
            if (startValue == endValue)
            {
                continue;
            }

            interior.Clear();

            var start = vertices[startValue].Position;
            var end = vertices[endValue].Position;
            var ab = RealVector.FromPoints(in start, in end);
            double abLenSq = ab.Dot(in ab);
            if (abLenSq <= 0.0)
            {
                AddEdgeDedup(startValue, endValue);
                continue;
            }

            for (int v = 0; v < vertices.Count; v++)
            {
                if (v == startValue || v == endValue)
                {
                    continue;
                }

                var p = vertices[v].Position;
                var ap = RealVector.FromPoints(in start, in p);
                double t = ap.Dot(in ab) / abLenSq;
                if (t <= tEps || t >= 1.0 - tEps)
                {
                    continue;
                }

                var closest = Lerp(in start, in end, t);
                if (p.DistanceSquared(in closest) > distanceEpsilonSquared)
                {
                    continue;
                }

                interior.Add((t, v));
            }

            if (interior.Count == 0)
            {
                AddEdgeDedup(startValue, endValue);
                continue;
            }

            interior.Sort(static (a, b) => a.T.CompareTo(b.T));

            int prev = startValue;
            for (int i = 0; i < interior.Count; i++)
            {
                int next = interior[i].VertexValue;
                AddEdgeDedup(prev, next);
                prev = next;
            }
            AddEdgeDedup(prev, endValue);
        }

        return output;
    }

    private static RealPoint Lerp(in RealPoint a, in RealPoint b, double t)
    {
        return new RealPoint(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
    }

}