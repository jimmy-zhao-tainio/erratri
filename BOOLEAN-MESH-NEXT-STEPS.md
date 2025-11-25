# Boolean Mesher Status & Concerns

This document captures the current state of the boolean mesher, the temporary hacks in place, and the risks to address.

## Current State
- Pipeline implemented: intersection -> triangle subdivision (PSLG) -> patch classification -> per-op patch selection -> mesh assembly -> STL export (Demo.Mesh union).
- Visual output for sphere union looks watertight in a quick inspection.

## Temporary Hacks / Disabled Checks
- `BooleanMeshAssembler`: manifold edge validation is commented out to allow export despite missing twin edges.
- `Demo.Mesh`: skips degenerate triangles when converting assembled mesh back to `Triangle` to avoid zero-normal errors.
- `PslgBuilder.SelectInteriorFaces`: added guard to treat all faces as interior if total face area matches the triangle but outer-face removal breaks the area check.

## Known Issues & Risks
- Non-manifold edges: with validation on, Demo.Mesh fails (e.g., edge (83,84) used once). Likely missing a twin patch or lost edge during assembly.
- PSLG outer-face selection: we hit cases where the "largest area is outer" heuristic failed; guard masks this but root cause is unknown (cycle orientation/duplication? boundary splitting?).
- Degenerate triangles: assembly can produce degenerate facets when normals are zero (filtered in demo only).
- Tolerances: heavy reliance on quantization at `TrianglePredicateEpsilon`; may need review when we tackle snapping to Z³.

## Suggested Next Steps
1) Re-enable manifold validation and fix the missing twin edge:
   - Use `boolean_mesh_nonmanifold_dump.txt` (edge 83–84, triangle 115 coords) to trace source patches and find the missing neighbor.
   - Add a deterministic test that reproduces this non-manifold in assembly.
2) Investigate PSLG outer-face mis-selection:
   - Build a test from the captured triangleIndex=84 PSLG inputs and inspect faces/cycles to find why the outer face isn’t the largest or why duplicate cycles appear.
   - Fix cycle orientation/selection so the guard in `SelectInteriorFaces` can be removed.
3) Remove temporary degenerate filtering in Demo.Mesh once manifold issues are resolved; add robust handling or prevention of degenerate patches in assembly.
4) Eventually, integrate Z³ snapping:
   - Snap vertices post-assembly using a quantized grid.
   - Ensure manifoldness and no cracks after snapping.

## Open Questions
- Are we losing boundary splits on triangle edges in topology extraction, leading to bad PSLGs?
- Do we need a more robust outer-face selection based on winding/bbox instead of area-only?
- Should assembly reuse original/global vertex IDs to reduce duplication and improve robustness?
