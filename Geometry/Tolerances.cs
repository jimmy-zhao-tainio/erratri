namespace Geometry;

// Centralized numerical tolerances for double-based operations.
public static class Tolerances
{
    // Epsilon for plane-side tests and related dot evaluations.
    public const double PlaneSideEpsilon = 1e-12;

    // Epsilon for triangle intersection / projection predicates
    // (2D barycentric checks, segment intersection, collinearity, uniqueness).
    public const double TrianglePredicateEpsilon = 1e-12;

    // Epsilon for detecting degenerate triangles based on squared area
    // (squared magnitude of the cross product of two edges).
    public const double DegenerateTriangleAreaEpsilonSquared = 1e-12;

    // Epsilon for world-space vertex merging in mesh assembly/auditing.
    public const double MergeEpsilon = 1e-12;
    public const double MergeEpsilonSquared = MergeEpsilon * MergeEpsilon;

    // Generic vertex-distance epsilon for 2D comparisons.
    public const double EpsVertex = TrianglePredicateEpsilon;
    public const double EpsVertexSquared = EpsVertex * EpsVertex;

    // Generic "near corner" tolerance for snapping in planar charts.
    public const double EpsCorner = 1e-7;

    // Generic "on side" tolerance for boundary classification in planar charts.
    public const double EpsSide = 1e-7;

    // PSLG-level vertex merge tolerance in param space.
    public const double PslgVertexMergeEpsilon = 1e-7;
    public const double PslgVertexMergeEpsilonSquared = PslgVertexMergeEpsilon * PslgVertexMergeEpsilon;

    // Generic area epsilon for 2D orientation/area checks.
    public const double EpsArea = TrianglePredicateEpsilon;

    // Feature-level tolerances for intersection geometry built on top of
    // the predicate layer. These are initially derived from the predicate
    // epsilon but can be tuned independently if needed.

    // Used when merging points in world space (3D and projected 2D) for
    // feature construction.
    public const double FeatureWorldDistanceEpsilonSquared =
        TrianglePredicateEpsilon * TrianglePredicateEpsilon;

    // Used for inclusive barycentric inside tests (U, V, W >= 0 and U+V+W == 1).
    public const double BarycentricInsideEpsilon = 1e-9;

    // Used when comparing barycentric coordinates (U, V, W) on triangles
    // when deduplicating feature-layer vertices.
    public const double FeatureBarycentricEpsilon = TrianglePredicateEpsilon;
}
