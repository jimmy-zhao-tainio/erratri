namespace Delaunay2D
{
    /// <summary>
    /// Optional tuning flags for constraint enforcement.
    /// </summary>
    public sealed class ConstraintEnforcer2DOptions
    {
        /// <summary>
        /// When true, performs a local Delaunay relaxation (edge flips only) inside
        /// the corridor patch after re-triangulation.
        /// Default is false.
        /// </summary>
        public bool RelaxToDelaunayAfterInsert { get; init; } = false;
    }
}
