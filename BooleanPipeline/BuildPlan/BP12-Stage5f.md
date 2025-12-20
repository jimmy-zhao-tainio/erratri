# BP12 — Stage5f (lift triangles + emit patches)

## Goal
Lift planar triangulations back to 3D and emit patch sets for the boolean assembly path.

## Owns
- (none; emits DTOs owned by BP06 via `Contracts.Stage5to6`)

## Depends on
- BP00
- BP11 (for `Contracts.Stage5eTo5f`)
- BP06 (for `Contracts.Stage5to6` DTO definitions)

## Deliverables
- [ ] Stage5f implementation project that:
  - [ ] Consumes `Contracts.Stage5eTo5f` triangulation DTOs
  - [ ] Lifts triangles to 3D using explicit mappings declared by upstream contracts
  - [ ] Emits `Contracts.Stage5to6` patch-set DTOs with deterministic ordering/IDs
  - [ ] Rejects non-liftable/ambiguous cases with stable error codes

## Done when
- [ ] Stage5f tests cover lifting correctness + determinism for representative faces

