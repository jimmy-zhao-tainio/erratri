namespace Geometry.Topology;

// Classifies the intersection of two closed, nondegenerate triangles in 3D.
// Triangles are treated as closed convex sets (including edges and vertices).
// Each pair of triangles falls into exactly one case.
public enum IntersectionType
{
    // The triangles are disjoint; their intersection set is empty.
    None = 0,

    // The triangles touch in exactly one point (0D contact).
    Point = 1,

    // The triangles intersect in a single non-degenerate segment (1D).
    // This includes both coplanar and non-coplanar segment intersections,
    // whether or not the segment coincides with a full edge of either triangle.
    Segment = 2,

    // Coplanar triangles overlap in area (2D intersection).
    // This includes partial overlap, containment, and equality.
    Area = 3
}

