using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    /// <summary>
    /// Optional debug options for Delaunay2DTriangulator, including corridor dumps.
    /// </summary>
    public sealed class Delaunay2DDebugOptions
    {
        /// <summary>
        /// When true, corridor construction/enforcement will emit debug dumps.
        /// </summary>
        public bool EnableCorridorDump { get; init; } = false;

        /// <summary>
        /// Optional callback to receive corridor dump data.
        /// If null and EnableCorridorDump is true, a default handler writes to Console.
        /// </summary>
        public Action<CorridorDump>? CorridorDumpHandler { get; init; }
    }

    /// <summary>
    /// Structured debug snapshot of a single constrained edge corridor.
    /// </summary>
    public sealed record CorridorDump(
        Edge2D AB,
        int[] OuterRing,
        IReadOnlyList<int[]> InnerRings,
        IReadOnlyList<int> CorridorTriangles,
        IReadOnlyList<RealPoint2D> Points,
        IReadOnlyList<Triangle2D> MeshBefore,
        IReadOnlyList<Triangle2D> MeshAfter);
}
