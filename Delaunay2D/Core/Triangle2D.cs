using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    // Invariant: A,B,C are always stored counter-clockwise in 2D; degenerate (collinear) triangles are never constructed.
    internal readonly struct Triangle2D
    {
        public int A { get; }
        public int B { get; }
        public int C { get; }

        internal Triangle2D(int a, int b, int c, IReadOnlyList<RealPoint2D> points)
        {
            if (points is null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (a == b || b == c || a == c)
            {
                throw new InvalidOperationException("Triangle vertices must be distinct.");
            }

            var pa = points[a];
            var pb = points[b];
            var pc = points[c];
            var orient = Geometry2DPredicates.Orientation(in pa, in pb, in pc);
            if (orient == OrientationKind.Collinear)
            {
                throw new InvalidOperationException("Degenerate triangle: vertices are collinear within tolerance.");
            }

            if (orient == OrientationKind.CounterClockwise)
            {
                A = a;
                B = b;
                C = c;
            }
            else
            {
                // Swap to enforce CCW storage.
                A = a;
                B = c;
                C = b;
            }
        }

        internal static double DistanceSquared(in RealPoint2D p, in RealPoint2D q)
        {
            double dx = p.X - q.X;
            double dy = p.Y - q.Y;
            return dx * dx + dy * dy;
        }
    }
}
