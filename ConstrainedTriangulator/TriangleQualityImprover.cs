using System;
using System.Collections.Generic;
using Geometry;

namespace ConstrainedTriangulator
{
    /// <summary>
    /// Single-pass edge-flip improver for triangle quality (min angle).
    /// Runs after initial triangulation; no constraints are violated.
    /// </summary>
    internal static class TriangleQualityImprover
    {
        internal static void RunEdgeFlipPass(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> constraints,
            List<(int A, int B, int C)> triangles)
        {
            if (triangles.Count == 0)
                return;

            // Build constraint edge set (undirected)
            var constraintEdges = new HashSet<EdgeKey>();
            foreach (var seg in constraints)
            {
                constraintEdges.Add(EdgeKey.From(seg.A, seg.B));
            }

            // Build adjacency: edge -> list of (triangleIndex, oppositeVertex) once
            var edgeMap = BuildEdgeAdjacency(triangles);

            // Single pass over this snapshot; edgeMap is not mutated inside this loop
            foreach (var kvp in edgeMap)
            {
                var edge = kvp.Key;
                var adjList = kvp.Value;

                // Only interior, non-constraint edges with exactly 2 adjacent triangles
                if (adjList.Count != 2)
                    continue;

                if (constraintEdges.Contains(edge))
                    continue;

                int triIndex0 = adjList[0].TriangleIndex;
                int triIndex1 = adjList[1].TriangleIndex;

                if (triIndex0 < 0 || triIndex0 >= triangles.Count ||
                    triIndex1 < 0 || triIndex1 >= triangles.Count ||
                    triIndex0 == triIndex1)
                {
                    continue;
                }

                int a = edge.U;
                int b = edge.V;
                int c = adjList[0].OppositeVertex;
                int d = adjList[1].OppositeVertex;

                if (c == d)
                    continue;

                if (!IsConvexQuad(points, a, c, b, d))
                    continue;

                var newEdge = EdgeKey.From(c, d);
                if (constraintEdges.Contains(newEdge))
                    continue;

                if (EdgeCrossesAnyConstraint(points, c, d, constraints))
                    continue;

                double before = MinAngleOfQuad(points, a, c, b, d, useDiagonalAC: true);
                double after = MinAngleOfQuad(points, a, c, b, d, useDiagonalAC: false);

                if (after <= before + 1e-9)
                    continue;

                // Flip: replace edge (a,b) with (c,d)
                triangles[triIndex0] = (c, d, a);
                triangles[triIndex1] = (d, c, b);
            }
        }

        private static Dictionary<EdgeKey, List<Adjacency>> BuildEdgeAdjacency(List<(int A, int B, int C)> triangles)
        {
            var map = new Dictionary<EdgeKey, List<Adjacency>>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var (a, b, c) = triangles[i];

                AddEdge(map, a, b, c, i);
                AddEdge(map, b, c, a, i);
                AddEdge(map, c, a, b, i);
            }

            return map;
        }

        private static void AddEdge(
            Dictionary<EdgeKey, List<Adjacency>> map,
            int u, int v, int opposite, int triIndex)
        {
            var key = EdgeKey.From(u, v);
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<Adjacency>(2);
                map.Add(key, list);
            }

            list.Add(new Adjacency(triIndex, opposite));
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public readonly int U;
            public readonly int V;

            private EdgeKey(int u, int v)
            {
                U = u;
                V = v;
            }

            public static EdgeKey From(int a, int b)
            {
                return (a <= b) ? new EdgeKey(a, b) : new EdgeKey(b, a);
            }

            public bool Equals(EdgeKey other) => U == other.U && V == other.V;

            public override bool Equals(object? obj) => obj is EdgeKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(U, V);
        }

        private readonly struct Adjacency
        {
            public readonly int TriangleIndex;
            public readonly int OppositeVertex;

            public Adjacency(int triangleIndex, int oppositeVertex)
            {
                TriangleIndex = triangleIndex;
                OppositeVertex = oppositeVertex;
            }
        }

        private static bool IsConvexQuad(
            IReadOnlyList<RealPoint2D> points,
            int a, int c, int b, int d)
        {
            // We treat quad as a-c-b-d and require same sign for both diagonals' adjacent triangles.
            var pa = points[a];
            var pb = points[b];
            var pc = points[c];
            var pd = points[d];

            double o1 = Orientation(pa, pc, pb);
            double o2 = Orientation(pa, pb, pd);

            // strictly same sign, not zero/degenerate
            return o1 * o2 > 0.0;
        }

        private static double Orientation(RealPoint2D a, RealPoint2D b, RealPoint2D c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static bool EdgeCrossesAnyConstraint(
            IReadOnlyList<RealPoint2D> points,
            int i, int j,
            IReadOnlyList<(int A, int B)> constraints)
        {
            var p1 = points[i];
            var p2 = points[j];

            for (int k = 0; k < constraints.Count; k++)
            {
                var (ca, cb) = constraints[k];

                // ignore constraints that share an endpoint with new edge
                if (ca == i || ca == j || cb == i || cb == j)
                    continue;

                var q1 = points[ca];
                var q2 = points[cb];

                if (ProperSegmentsIntersect(p1, p2, q1, q2))
                    return true;
            }

            return false;
        }

        private static bool ProperSegmentsIntersect(
            RealPoint2D p1, RealPoint2D p2,
            RealPoint2D q1, RealPoint2D q2)
        {
            double o1 = Orientation(p1, p2, q1);
            double o2 = Orientation(p1, p2, q2);
            double o3 = Orientation(q1, q2, p1);
            double o4 = Orientation(q1, q2, p2);

            return o1 * o2 < 0.0 && o3 * o4 < 0.0;
        }

        private static double MinAngleOfQuad(
            IReadOnlyList<RealPoint2D> points,
            int a, int c, int b, int d,
            bool useDiagonalAC)
        {
            // Quad vertices: a, c, b, d (conceptually).
            // If useDiagonalAC: triangles are (a,c,b) and (a,d,c) or similar.
            // For simplicity, we rebuild explicit triangles for both cases.

            if (useDiagonalAC)
            {
                double m1 = MinAngleOfTriangle(points, a, c, b);
                double m2 = MinAngleOfTriangle(points, a, d, c);
                return Math.Min(m1, m2);
            }
            else
            {
                double m1 = MinAngleOfTriangle(points, c, d, a);
                double m2 = MinAngleOfTriangle(points, d, c, b);
                return Math.Min(m1, m2);
            }
        }

        private static double MinAngleOfTriangle(
            IReadOnlyList<RealPoint2D> points,
            int ia, int ib, int ic)
        {
            var a = points[ia];
            var b = points[ib];
            var c = points[ic];

            double ab2 = Dist2(a, b);
            double bc2 = Dist2(b, c);
            double ca2 = Dist2(c, a);

            if (ab2 <= 0.0 || bc2 <= 0.0 || ca2 <= 0.0)
                return 0.0;

            double angleA = AngleFromSides(bc2, ca2, ab2);
            double angleB = AngleFromSides(ca2, ab2, bc2);
            double angleC = AngleFromSides(ab2, bc2, ca2);

            return Math.Min(angleA, Math.Min(angleB, angleC));
        }

        private static double Dist2(RealPoint2D p, RealPoint2D q)
        {
            double dx = p.X - q.X;
            double dy = p.Y - q.Y;
            return dx * dx + dy * dy;
        }

        private static double AngleFromSides(double b2, double c2, double a2)
        {
            // a is opposite the angle, b and c are adjacent (squared)
            double num = b2 + c2 - a2;
            double den = 2.0 * Math.Sqrt(b2 * c2);
            if (den <= 0.0)
                return 0.0;

            double cos = num / den;
            cos = Math.Clamp(cos, -1.0, 1.0);
            return Math.Acos(cos); // radians
        }
    }
}
