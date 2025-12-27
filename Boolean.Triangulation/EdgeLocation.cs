namespace Boolean;

// Location of a point on the reference triangle, expressed in terms of
// which oriented edge it lies on (if any). The numbering matches the
// TRIANGLESUBDIVISION roadmap:
//
//   - Edge0: edge V0 -> V1
//   - Edge1: edge V1 -> V2
//   - Edge2: edge V2 -> V0
//   - Interior: strictly inside or numerically away from all edges.
public enum EdgeLocation
{
    Interior = 0,
    Edge0 = 1,
    Edge1 = 2,
    Edge2 = 3
}
