using System;
using System.Collections.Generic;

namespace Pslg.Phases;

internal static class PslgHalfEdgePhase
{
    // Phase #2: build half-edges with twin links and assign Next using angular ordering of outgoing half-edges per vertex.
    internal static PslgHalfEdgeState Run(PslgBuildState buildState)
    {
        if (buildState.Vertices is null) throw new ArgumentNullException(nameof(buildState));
        if (buildState.Edges is null) throw new ArgumentNullException(nameof(buildState));

        var vertices = buildState.Vertices;
        var edges = buildState.Edges;

        var halfEdges = new List<PslgHalfEdge>(edges.Count * 2);

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            int idx = halfEdges.Count;
            halfEdges.Add(new PslgHalfEdge
            {
                From = e.Start,
                To = e.End,
                Twin = idx + 1,
                Next = -1,
                IsBoundary = e.IsBoundary
            });

            halfEdges.Add(new PslgHalfEdge
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

        return new PslgHalfEdgeState(vertices, edges, halfEdges);
    }
}
