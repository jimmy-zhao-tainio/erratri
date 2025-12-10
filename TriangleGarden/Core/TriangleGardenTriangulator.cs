using System;
using System.Collections.Generic;
using Geometry;

namespace TriangleGarden
{
    /// <summary>
    /// 2D constrained triangulator over Geometry.RealPoint2D.
    /// Coordinates are planar; no world-space mapping here.
    /// </summary>
    public static class TriangleGardenTriangulator
    {
        public static TriangleGardenResult Run(in TriangleGardenInput input, bool validate = false)
        {
            TriangleGardenInputValidator.Validate(in input);

            var segments = Triangulate(input);
            var triangles = TriangleGardenMeshBuilder.BuildTrianglesFromEdges(segments, input.Points);

            if (validate)
            {
                // TODO: add validation hooks for the generated mesh.
            }

            return new TriangleGardenResult(input.Points, triangles);
        }

        public static List<(int A, int B)> Triangulate(TriangleGardenInput input)
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

                    bool addedForThisEdge = false;

                    foreach (int p3 in candidates)
                    {
                        if (p3 == p1 || p3 == p2)
                            continue;

                        if (Enforce.IsLegalTriangle(p1, p2, p3, points, segments))
                        {
                            if (TriangleGardenEdges.AddTriangleEdges(p1, p2, p3, segments, adjacency))
                            {
                                changed = true;
                                addedForThisEdge = true;
                            }
                        }
                    }

                    if (!addedForThisEdge)
                    {
                        int n = points.Count;
                        for (int p3 = 0; p3 < n; p3++)
                        {
                            if (p3 == p1 || p3 == p2)
                                continue;
                            if (candidates.Contains(p3))
                                continue;

                            if (Enforce.IsLegalTriangle(p1, p2, p3, points, segments))
                            {
                                if (TriangleGardenEdges.AddTriangleEdges(p1, p2, p3, segments, adjacency))
                                {
                                    changed = true;
                                    addedForThisEdge = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            } while (changed);

            return segments;
        }
    }
}
