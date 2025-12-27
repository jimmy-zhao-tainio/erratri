namespace Boolean;

// High-level classification of the intersection-segment pattern on a
// triangle. This is a deliberately small set for the "fast lane":
//
//   - None: no segments.
//   - SingleEdgeToEdge: exactly one segment whose endpoints lie on two
//     distinct edges (no interior endpoints).
//   - Other: everything else (to be handled by the general PSLG lane).
public enum PatternKind
{
    None = 0,
    SingleEdgeToEdge = 1,
    Other = 2
}
