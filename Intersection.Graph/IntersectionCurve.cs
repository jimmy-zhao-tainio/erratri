using System;
using System.Collections.Immutable;

namespace Kernel;

public sealed class IntersectionCurve
{
    public ImmutableArray<IntersectionVertexId> Vertices { get; }
    public ImmutableArray<IntersectionEdgeId> Edges { get; }

    // One flag per edge indicating whether this edge is a synthetic
    // closure edge added by the regularizer rather than a real edge
    // from the global IntersectionGraph.
    public ImmutableArray<bool> IsClosureEdge { get; }

    public double TotalLength { get; }

    public IntersectionCurve(
        ImmutableArray<IntersectionVertexId> vertices,
        ImmutableArray<IntersectionEdgeId> edges,
        ImmutableArray<bool> isClosureEdge,
        double totalLength)
    {
        if (vertices.IsDefaultOrEmpty)
            throw new ArgumentException("Vertices must be non-empty.", nameof(vertices));

        if (edges.IsDefault)
            throw new ArgumentException("Edges must be initialized.", nameof(edges));

        if (isClosureEdge.IsDefault)
            throw new ArgumentException("IsClosureEdge must be initialized.", nameof(isClosureEdge));

        if (edges.Length != vertices.Length - 1)
            throw new ArgumentException("Edges length must be vertices length minus one.", nameof(edges));

        if (isClosureEdge.Length != edges.Length)
            throw new ArgumentException("IsClosureEdge length must match edges length.", nameof(isClosureEdge));

        if (vertices[0].Value != vertices[^1].Value)
            throw new ArgumentException("Vertices must form a closed cycle (first == last).", nameof(vertices));

        Vertices = vertices;
        Edges = edges;
        IsClosureEdge = isClosureEdge;
        TotalLength = totalLength;
    }
}

