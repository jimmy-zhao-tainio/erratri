using System;
using System.Collections.Generic;

namespace ConstrainedTriangulator
{
    internal static class Edges
    {
        internal static bool ContainsEdge(
            IReadOnlyList<(int A, int B)> segments,
            int a,
            int b)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                if ((s.A == a && s.B == b) || (s.A == b && s.B == a))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool AddTriangleEdges(
            int a,
            int b,
            int c,
            List<(int A, int B)> segments)
        {
            bool added = false;

            void Add(int x, int y)
            {
                if (!ContainsEdge(segments, x, y))
                {
                    segments.Add((x, y));
                    added = true;
                }
            }

            Add(a, b);
            Add(b, c);
            Add(c, a);

            return added;
        }

        internal static bool AddTriangleEdges(
            int a,
            int b,
            int c,
            List<(int A, int B)> segments,
            Dictionary<int, HashSet<int>> adjacency)
        {
            bool added = false;

            void Add(int x, int y)
            {
                if (!ContainsEdge(segments, x, y))
                {
                    segments.Add((x, y));
                    adjacency[x].Add(y);
                    adjacency[y].Add(x);
                    added = true;
                }
            }

            Add(a, b);
            Add(b, c);
            Add(c, a);

            return added;
        }

        internal static HashSet<int> CollectNeighbors(
            int p1,
            int p2,
            IReadOnlyList<(int A, int B)> segments)
        {
            var set = new HashSet<int>();
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];

                if (s.A == p1) set.Add(s.B);
                else if (s.B == p1) set.Add(s.A);

                if (s.A == p2) set.Add(s.B);
                else if (s.B == p2) set.Add(s.A);
            }
            return set;
        }

    }
}
