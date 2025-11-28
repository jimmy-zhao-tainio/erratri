## PSLG / Ear Clipping

- `TriangulateSimple` must tolerate collinear chains along polygon edges and use `Tolerances.EpsArea` for convex/inside tests. The collinear-path fan + ear clipping changes fixed that.
- Diagnostics that helped:
  - Epsilon-based vertex canonicalizer feeds the manifold validator and A/B edge auditor.
  - A/B edge auditor: counts per side, flags one-sided edges.
  - Canonical edge-chain idea: ordered list of vertices per original mesh edge to avoid T-junctions; auditor compares triangle edge chains (now tolerance for reversed order).

## Cheese bug findings

- The 40 non-manifold edges are one-sided (provenance shows mostly A-only).
- Example edge `(-100,67,-100) ↔ (-67,67,-100)`: there are 2 original A triangles on this box edge, but only 1 A patch appears in the captured BooleanPatchSet → missing A patch. This is a patch construction/classification issue, not triangulation or edge-chain orientation.
- Edge-chain injection and triangulation appear correct; the missing mate triangle causes single-use edges.

## Avoid next time

- Don’t modify BooleanOps/BooleanMeshAssembler without a regression harness (sphere tests + Demo.Mesh) running.
- Don’t stack auditors endlessly; lock in tests and change one layer at a time.
