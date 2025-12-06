using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal readonly struct SuperTriangle
    {
        public RealPoint2D A { get; }
        public RealPoint2D B { get; }
        public RealPoint2D C { get; }

        public SuperTriangle(IReadOnlyList<RealPoint2D> points)
        {
            if (points is null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            double minX = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            double spanX = maxX - minX;
            double spanY = maxY - minY;
            double span = Math.Max(spanX, spanY);
            if (span == 0)
            {
                throw new ArgumentException("Cannot build a super triangle for degenerate point set.", nameof(points));
            }

            double centerX = 0.5 * (minX + maxX);
            double centerY = 0.5 * (minY + maxY);
            double radius = 10.0 * span;

            A = new RealPoint2D(centerX, centerY + 2 * radius);
            B = new RealPoint2D(centerX - radius, centerY - radius);
            C = new RealPoint2D(centerX + radius, centerY - radius);
        }

        /// <summary>
        /// Append the super-triangle vertices to the provided point list and return the seeded triangle.
        /// </summary>
        internal Triangle2D AppendTo(List<RealPoint2D> pointBuffer)
        {
            if (pointBuffer is null)
            {
                throw new ArgumentNullException(nameof(pointBuffer));
            }

            int startIndex = pointBuffer.Count;
            pointBuffer.Add(A);
            pointBuffer.Add(B);
            pointBuffer.Add(C);

            return new Triangle2D(startIndex, startIndex + 1, startIndex + 2, pointBuffer);
        }
    }
}
