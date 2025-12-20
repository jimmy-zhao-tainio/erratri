# BP08 — Stage5b (embed constraints to 2D) + `Contracts.Stage5bTo5c`

## Goal
Embed 3D per-face constraints into a face-local 2D coordinate system suitable for planar processing.

## Owns
- `Contracts.Stage5bTo5c`

## Depends on
- BP00
- BP07 (for `Contracts.Stage5aTo5b`)

## Deliverables
- [ ] `Contracts.Stage5bTo5c`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage5b implementation project that:
  - [ ] Defines and emits a deterministic face-local 2D embedding (orientation + units per contract)
  - [ ] Preserves required mappings back to 3D for downstream lifting

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage5b tests confirm stable embedding/orientation rules

