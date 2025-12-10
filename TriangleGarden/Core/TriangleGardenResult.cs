using System;
using System.Collections.Generic;
using Geometry;

namespace TriangleGarden
{
    /// <summary>
    /// Output from the TriangleGarden triangulator.
    /// </summary>
    public sealed class TriangleGardenResult
    {
        /// <summary>
        /// The 2D coordinates of the triangulation vertices.
        /// Typically this is the same sequence as the input Points, but the
        /// implementation is allowed to add Steiner points if needed.
        /// </summary>
        public IReadOnlyList<RealPoint2D> Points { get; }

        /// <summary>
        /// Triangles as index triples into Points.
        /// </summary>
        public IReadOnlyList<(int A, int B, int C)> Triangles { get; }

        public TriangleGardenResult(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B, int C)> triangles)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
        }
    }
}
