using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Geometry.Predicates;

namespace Kernel.Pslg.Phases;

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
            var innerCycles = new List<int[]>();
            var innerCycleKeys = new HashSet<string>();
            if (children[i] != null)
            {
                foreach (var ch in children[i])
                {
                    if (depth[ch] == depth[i] + 1)
                    {
                        var key = CanonicalFaceKey(norm[ch].Vertices);
                        if (innerCycleKeys.Add(key))
                        {
                            innerCycles.Add(norm[ch].Vertices);
                        }
                    }
                }
            }

            double innerCycleAreaSum = 0.0;
            foreach (var innerCycle in innerCycles)
            {
                innerCycleAreaSum += CycleArea(vertices, innerCycle);
            }

            double signedArea = norm[i].Area - innerCycleAreaSum;
            if (Math.Abs(signedArea) <= Tolerances.EpsArea)
            {
                continue;
            }

            faces.Add(new PslgFace(norm[i].Vertices, innerCycles, signedArea));
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
}
