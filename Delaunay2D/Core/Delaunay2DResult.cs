using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    /// <summary>
    /// Output from the 2D Delaunay triangulator.
    /// </summary>
    public sealed class Delaunay2DResult
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

        public Delaunay2DResult(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B, int C)> triangles)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
        }
    }
}
