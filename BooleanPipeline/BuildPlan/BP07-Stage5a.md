# BP07 — Stage5a (collect per-face constraints) + `Contracts.Stage5aTo5b`

## Goal
Collect 3D per-face constraints from the Stage4→Stage5 intersection graph.

## Owns
- `Contracts.Stage5aTo5b`

## Depends on
- BP00
- BP05 (for `Contracts.Stage4to5`)

## Deliverables
- [ ] `Contracts.Stage5aTo5b`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage5a implementation project that:
  - [ ] Consumes `Contracts.Stage4to5` graph + Stage5 policies from input
  - [ ] Emits 3D constraint DTOs required by Stage5b
  - [ ] Rejects unsupported constraint configurations explicitly (stable codes)

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage5a tests cover representative constraint edge cases

