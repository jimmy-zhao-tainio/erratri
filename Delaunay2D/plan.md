1) Tolerances and robustness (orientation / circumcircle)
- One epsilon: use Geometry.Tolerances.TrianglePredicateEpsilon for orientation and circumcircle.
- Orientation 2D: cross = (qx - px) * (ry - py) - (qy - py) * (rx - px); cross > +eps => CCW; cross < -eps => CW; |cross| <= eps => collinear.
- Circumcircle: standard 2D determinant (z=0). Use eps margin. If within +-eps, treat as on-circle (not strictly inside) to avoid flip oscillations.
- Cavity/flip: triangle is “bad” only if circumcircle value < -eps; between -eps and +eps is allowed.
- Naming: prefer full descriptive variable names (e.g., determinant instead of det) for clarity and consistency with Geometry conventions.

2) Super-triangle bounds
- Points first, super vertices last; padding factor k = 10.
- BBox: minX/maxX/minY/maxY; span = max(dx, dy). If span == 0, handle as degeneracy.
- Center (cx, cy) = midpoint of bbox; R = k * span.
- Super vertices: S0 = (cx, cy + 2R); S1 = (cx - R, cy - R); S2 = (cx + R, cy - R).
- Indices: original points [0..N-1]; super vertices appended at [N..N+2].
- Finalization: drop any triangle that references index >= N; result Points list is exactly the original inputs.

3) Cavity boundary (Bowyer–Watson)
- For each bad triangle (circumcircle contains P with rule above), push its 3 undirected edges as key (min,max) into a map<count>.
- Boundary edges are those with count == 1. No need to order; fan-fill with triangles (P, A, B) after orientation normalization.

4) Triangle data model and orientation
- Triangle2D stored CCW only. After creating (A,B,C), compute signed area; if area < -eps, swap B/C; if |area| <= eps, discard as degenerate.

5) Point location performance
- Start with O(n^2) point insert: scan all triangles for containing triangle (barycentric or area signs).
- If not found (degenerate cases), optionally bbox prefilter or relaxed scan. Leave hooks/comments for future accel (grid or walking), not implemented now.

6) Degeneracy handling (duplicates, collinear, tiny spans)
- Upfront checks in Run:
  - Throw if Points.Count < 3.
  - Detect duplicates via GridRounding.Snap or epsilon (Tolerances.EpsVertex); v1: throw on duplicates.
  - All-collinear check using orientation with eps; if all collinear, throw with clear message.

7) Invariants timing
- Debug-only checks.
- Per insert (or every K=10): all triangles CCW; optionally ensure no super vertex leaks except during bootstrap.
- After full triangulation (pre-constraints): edge manifoldness (each undirected edge count 1 or 2). Optional random Delaunay spot-checks. Wrap under #if DEBUG or a flag.

8) Constraint corridor (“rip and ear-clip”)
- For each segment (A,B):
  - If edge exists, mark constrained (HashSet of normalized edges) and continue.
  - Else find triangles intersected by AB (segment/segment with eps; include cases where vertices lie on AB). Remove them.
  - Build corridor boundary via edge counts; edges with count == 1 form boundary, ensure AB is present.
  - Order boundary into a ring: build adjacency (each boundary vertex has two neighbors), walk from A to B and around to form the loop; flip orientation if needed before ear-clipping.
  - Triangulate corridor via TriangulateSimple on the ordered ring; insert triangles; keep AB constrained; future flips must not flip constrained edges.

9) TriangulateSimple reuse in 2D
- Add internal TriangulateSimpleRing(IReadOnlyList<int> ring, IReadOnlyList<RealPoint2D> points).
- Validate ring: no duplicate indices except optional closing vertex; if closed, drop last before triangulation.
- Use pure 2D area/orientation (shoelace). Prefer direct 2D math; z=0 RealTriangle only if convenient.

10) Testing plan
- Base: 3-point triangle; convex quad; 5–8 random points.
- Near-degenerate: almost-collinear sets (expect throw or very thin but stable); duplicate points => throw.
- Constraints: hull-edge segment; diagonal across quad; inner small triangle loop inside outer triangle—assert loop edges present, no crossings, area sum ≈ outer.
- Later fuzz: random points + simple non-crossing segments; assert manifoldness, no degenerate tris, all constraints present, no constraint crossings.
