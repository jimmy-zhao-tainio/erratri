using System;
using Geometry;

namespace ConstrainedTriangulator
{
    internal static class InputValidator
    {
        internal static void Validate(in Input input)
        {
            if (input.Points is null)
            {
                throw new ArgumentNullException(nameof(input.Points));
            }

            if (input.Segments is null)
            {
                throw new ArgumentNullException(nameof(input.Segments));
            }

            int pointCount = input.Points.Count;
            if (pointCount < 3)
            {
                throw new ArgumentException("Need at least 3 points for triangulation.", nameof(input));
            }

            for (int i = 0; i < pointCount; i++)
            {
                var pi = input.Points[i];
                for (int j = i + 1; j < pointCount; j++)
                {
                    var pj = input.Points[j];
                    double dx = pi.X - pj.X;
                    double dy = pi.Y - pj.Y;
                    double dist2 = dx * dx + dy * dy;
                    if (dist2 <= Tolerances.EpsVertexSquared)
                    {
                        throw new ArgumentException("ConstrainedTriangulatorInput contains duplicate or near-duplicate points.", nameof(input));
                    }
                }
            }

            var first = input.Points[0];
            var second = input.Points[1];
            bool allCollinear = true;
            for (int i = 2; i < pointCount; i++)
            {
                var current = input.Points[i];
                double orient = (second.X - first.X) * (current.Y - first.Y) - (second.Y - first.Y) * (current.X - first.X);
                if (Math.Abs(orient) > Tolerances.EpsArea)
                {
                    allCollinear = false;
                    break;
                }
            }

            if (allCollinear)
            {
                throw new ArgumentException("Cannot triangulate: all points are collinear within tolerance.", nameof(input));
            }
        }
    }
}
