using System;
using System.Collections.Generic;
using Geometry;

namespace ConstrainedTriangulator
{
    /// <summary>
    /// Input to the ConstrainedTriangulator triangulator.
    /// Coordinates are expressed in a single 2D plane coordinate system.
    /// </summary>
    public readonly struct Input
    {
        /// <summary>
        /// 2D coordinates of all points to be triangulated.
        /// </summary>
        public IReadOnlyList<RealPoint2D> Points { get; }

        /// <summary>
        /// Optional constrained segments, as index pairs into Points.
        /// Each segment (A,B) must not be crossed by any triangle edge in the final triangulation.
        /// </summary>
        public IReadOnlyList<(int A, int B)> Segments { get; }

        public Input(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }
    }
}
