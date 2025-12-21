using System;
using System.Collections.Generic;
using Geometry;

namespace ConstrainedTriangulator
{
    /// <summary>
    /// 2D constrained triangulator over Geometry.RealPoint2D.
    /// Coordinates are planar; no world-space mapping here.
    /// </summary>
    public static class Triangulator
    {
        public static Result Run(in Input input, bool validate = false)
        {
            InputValidator.Validate(in input);

            var segments = Triangulate(input);
            var triangles = MeshBuilder.BuildTrianglesFromEdges(segments, input.Points);

            if (validate)
            {
                Validator.ValidateFullTriangulation(input.Points, segments, triangles);
            }

            return new Result(input.Points, triangles);
        }

        public static Result RunFast(in Input input, bool validate = false)
        {
            InputValidator.Validate(in input);

            var segments = TriangulateFast(input);
            var triangles = MeshBuilder.BuildTrianglesFromEdges(segments, input.Points);

            if (validate)
            {
                Validator.ValidateFullTriangulation(input.Points, segments, triangles);
            }

            return new Result(input.Points, triangles);
        }

        public static List<(int A, int B)> Triangulate(Input input)
        {
            return TriangulateSlow(input);
        }

        public static List<(int A, int B)> TriangulateSlow(Input input)
        {
            var points = input.Points;
            var segments = new List<(int A, int B)>(input.Segments);

            if (points.Count < 3)
                return segments;

            bool changed;
            do
            {
                changed = false;

                // Only iterate over the edges that existed at the start of this sweep.
                int segmentCountAtStart = segments.Count;

                for (int si = 0; si < segmentCountAtStart; si++)
                {
                    var edge = segments[si];
                    int p1 = edge.A;
                    int p2 = edge.B;

                    int n = points.Count;
                    for (int p3 = 0; p3 < n; p3++)
                    {
                        if (p3 == p1 || p3 == p2)
                            continue;

                        if (Enforce.IsLegalTriangle(p1, p2, p3, points, segments))
                        {
                            if (Edges.AddTriangleEdges(p1, p2, p3, segments))
                            {
                                changed = true;
                            }
                        }
                        else if (Enforce.IsLegalTriangle(p2, p1, p3, points, segments))
                        {
                            if (Edges.AddTriangleEdges(p2, p1, p3, segments))
                            {
                                changed = true;
                            }
                        }
                    }
                }
            }
            while (changed);

            return segments;
        }

        public static List<(int A, int B)> TriangulateFast(Input input)
        {
            var points = input.Points;
            var segments = new List<(int A, int B)>(input.Segments);

            var adjacency = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < points.Count; i++)
            {
                adjacency[i] = new HashSet<int>();
            }

            foreach (var seg in segments)
            {
                adjacency[seg.A].Add(seg.B);
                adjacency[seg.B].Add(seg.A);
            }

            if (points.Count < 3)
                return segments;

            // 1) Fast adjacency-based saturation
            segments = TriangulateCore(input, segments, adjacency);

            // 2) One global completion sweep to bridge stubborn gaps
            RunGlobalCompletion(points, segments, adjacency);

            // 3) Final fast pass to absorb any new bridges
            segments = TriangulateCore(input, segments, adjacency);

            return segments;
        }

        private static List<(int A, int B)> TriangulateCore(
            Input input,
            List<(int A, int B)> segments,
            Dictionary<int, HashSet<int>> adjacency)
        {
            var points = input.Points;

            bool changed;
            do
            {
                changed = false;
                int segmentCountAtStart = segments.Count;

                for (int si = 0; si < segmentCountAtStart; si++)
                {
                    var edge = segments[si];
                    int p1 = edge.A;
                    int p2 = edge.B;

                    var candidates = new HashSet<int>();
                    if (adjacency.TryGetValue(p1, out var n1))
                    {
                        foreach (var v in n1) candidates.Add(v);
                    }
                    if (adjacency.TryGetValue(p2, out var n2))
                    {
                        foreach (var v in n2) candidates.Add(v);
                    }

                    foreach (int p3 in candidates)
                    {
                        if (p3 == p1 || p3 == p2)
                            continue;

                        if (Enforce.IsLegalTriangle(p1, p2, p3, points, segments))
                        {
                            if (Edges.AddTriangleEdges(p1, p2, p3, segments, adjacency))
                            {
                                changed = true;
                            }
                        }
                        else if (Enforce.IsLegalTriangle(p2, p1, p3, points, segments))
                        {
                            if (Edges.AddTriangleEdges(p2, p1, p3, segments, adjacency))
                            {
                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);

            return segments;
        }

        private static void RunGlobalCompletion(
            IReadOnlyList<RealPoint2D> points,
            List<(int A, int B)> segments,
            Dictionary<int, HashSet<int>> adjacency)
        {
            int n = points.Count;
            int edgeCountAtStart = segments.Count;

            for (int ei = 0; ei < edgeCountAtStart; ei++)
            {
                var edge = segments[ei];
                int p1 = edge.A;
                int p2 = edge.B;

                var local = new HashSet<int>();
                if (adjacency.TryGetValue(p1, out var n1))
                {
                    foreach (var v in n1) local.Add(v);
                }
                if (adjacency.TryGetValue(p2, out var n2))
                {
                    foreach (var v in n2) local.Add(v);
                }

                for (int p3 = 0; p3 < n; p3++)
                {
                    if (p3 == p1 || p3 == p2)
                        continue;
                    if (local.Contains(p3))
                        continue;

                    bool legalForward = Enforce.IsLegalTriangle(p1, p2, p3, points, segments);
                    bool legalReverse = !legalForward && Enforce.IsLegalTriangle(p2, p1, p3, points, segments);
                    if (legalForward || legalReverse)
                    {
                        int a = legalForward ? p1 : p2;
                        int b = legalForward ? p2 : p1;

                        if (Edges.AddTriangleEdges(a, b, p3, segments, adjacency))
                        {
                            // Bridge made; move to next edge to avoid overfanning
                            break;
                        }
                    }
                }
            }
        }
    }
}
