using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

public readonly struct IntersectionVertexId
{
    public int Value { get; }

    public IntersectionVertexId(int value)
    {
        Value = value;
    }

    public override string ToString() => $"v{Value}";
}

public readonly struct IntersectionEdgeId
{
    public int Value { get; }

    public IntersectionEdgeId(int value)
    {
        Value = value;
    }
}

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
        // barycentric coordinates on triangle A, then deduplicate by a
        // quantized world key so that shared endpoints across pairs collapse
        // to a single global IntersectionVertexId.
        var globalVertices = new List<(IntersectionVertexId, RealPoint)>();
        var vertexLookup = new Dictionary<QuantizedPointKey, IntersectionVertexId>();
        var pairVertexToGlobal = new Dictionary<(int PairIndex, int LocalVertexId), IntersectionVertexId>();

        var trianglesA = intersectionSet.TrianglesA;

        for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
        {
            var pair = pairs[pairIndex];
            var intersection = pair.Intersection;
            var triA = trianglesA[intersection.TriangleIndexA];

            var localVertices = pair.Vertices;
            for (int i = 0; i < localVertices.Count; i++)
            {
                var v = localVertices[i];
                var baryOnA = v.OnTriangleA;
                var world = triA.FromBarycentric(in baryOnA);

                var key = QuantizedPointKey.FromRealPoint(world);
                if (!vertexLookup.TryGetValue(key, out var globalId))
                {
                    globalId = new IntersectionVertexId(globalVertices.Count);
                    globalVertices.Add((globalId, world));
                    vertexLookup.Add(key, globalId);
                }

                pairVertexToGlobal[(pairIndex, v.VertexId.Value)] = globalId;
            }
        }

        // Global edge construction.
        // For each local PairSegment, map its endpoints to global vertices,
        // discard degenerate segments, then normalize as undirected edges and
        // deduplicate across the whole graph.
        var globalEdges = new List<(IntersectionEdgeId, IntersectionVertexId, IntersectionVertexId)>();
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

        return new IntersectionGraph(intersectionSet, globalVertices, globalEdges, pairs);
    }

    private readonly struct QuantizedPointKey : IEquatable<QuantizedPointKey>
    {
        public readonly long X;
        public readonly long Y;
        public readonly long Z;

        public QuantizedPointKey(long x, long y, long z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static QuantizedPointKey FromRealPoint(RealPoint point)
        {
            double invEpsilon = 1.0 / Tolerances.TrianglePredicateEpsilon;
            long qx = (long)Math.Round(point.X * invEpsilon);
            long qy = (long)Math.Round(point.Y * invEpsilon);
            long qz = (long)Math.Round(point.Z * invEpsilon);
            return new QuantizedPointKey(qx, qy, qz);
        }

        public bool Equals(QuantizedPointKey other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object? obj) => obj is QuantizedPointKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}
