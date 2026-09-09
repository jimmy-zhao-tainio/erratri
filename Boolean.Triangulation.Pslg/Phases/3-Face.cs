using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Geometry.Predicates;

namespace Pslg.Phases;

internal static class PslgFacePhase
{
    // Phase #3: walk faces using half-edge Next pointers; every half-edge belongs to exactly one directed face cycle.
    internal static PslgFaceState Run(PslgHalfEdgeState halfEdgeState)
    {
        if (halfEdgeState.Vertices is null) throw new ArgumentNullException(nameof(halfEdgeState));
        if (halfEdgeState.HalfEdges is null) throw new ArgumentNullException(nameof(halfEdgeState));

        var vertices = halfEdgeState.Vertices;
        var halfEdges = halfEdgeState.HalfEdges;

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

        var faces = BuildFaces(rawCycles, vertices);
        return new PslgFaceState(vertices, halfEdgeState.Edges, halfEdges, faces);
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

    private static List<PslgFace> BuildFaces(
        IReadOnlyList<RawCycle> cycles,
        IReadOnlyList<PslgVertex> vertices)
    {
        if (cycles.Count == 0) return new List<PslgFace>();

        // With the next-clockwise half-edge rule, bounded face boundaries
        // are CCW. A clockwise cycle is either the unbounded exterior or a
        // hole of a containing face. Never erase this topological distinction.
        var positive = cycles.Where(c => c.Area > Tolerances.EpsArea).ToArray();
        var holes = positive.Select(_ => new List<int[]>()).ToArray();
        foreach (var negative in cycles.Where(c => c.Area < -Tolerances.EpsArea))
        {
            int owner = -1;
            double bestArea = double.PositiveInfinity;
            for (int i = 0; i < positive.Length; i++)
            {
                var outer = positive[i];
                if (outer.Area <= -negative.Area + Tolerances.EpsArea || outer.Area >= bestArea) continue;
                var polygon = new RealPolygon(outer.Vertices.Select(id =>
                    new RealPoint(vertices[id].X, vertices[id].Y, 0)).ToList());
                if (!negative.Vertices.All(id => RealPolygonPredicates.ContainsInclusive(polygon,
                    new RealPoint(vertices[id].X, vertices[id].Y, 0)))) continue;
                owner = i;
                bestArea = outer.Area;
            }
            if (owner >= 0) holes[owner].Add(negative.Vertices);
        }
        var faces = new List<PslgFace>();
        for (int i = 0; i < positive.Length; i++)
        {
            double area = positive[i].Area - holes[i].Sum(h => CycleArea(vertices, h));
            if (area > Tolerances.EpsArea)
                faces.Add(new PslgFace(positive[i].Vertices, holes[i], area));
        }
        return faces;
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

}