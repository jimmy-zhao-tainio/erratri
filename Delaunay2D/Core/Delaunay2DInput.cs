using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    /// <summary>
    /// Input to the 2D Delaunay triangulator.
    /// Coordinates are in a single 2D plane coordinate system (e.g. triangle-plane coords).
    /// </summary>
    public readonly struct Delaunay2DInput
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

        /// <summary>
        /// Optional debug options. If null, debug features are disabled.
        /// </summary>
        public Delaunay2DDebugOptions? Debug { get; init; }

        /// <summary>
        /// Optional constraint enforcer options.
        /// </summary>
        public ConstraintEnforcer2DOptions? Options { get; init; }

        public Delaunay2DInput(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B)> segments)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }
    }
}
