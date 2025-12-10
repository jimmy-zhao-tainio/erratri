using System;
using System.Collections.Generic;

namespace TriangleGarden
{
    internal static class TriangleGardenEdges
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
            if (!ContainsEdge(segments, a, b)) { segments.Add((a, b)); added = true; }
            if (!ContainsEdge(segments, b, c)) { segments.Add((b, c)); added = true; }
            if (!ContainsEdge(segments, c, a)) { segments.Add((c, a)); added = true; }
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
