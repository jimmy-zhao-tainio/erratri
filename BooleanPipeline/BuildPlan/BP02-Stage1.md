# BP02 — Stage1 (preprocess meshes) + `Contracts.Stage1to2`

## Goal
Take the Stage0 context and produce normalized, validated meshes suitable for intersection work.

## Owns
- `Contracts.Stage1to2`

## Depends on
- BP00
- BP01 (for `Contracts.Stage0to1`)

## Deliverables
- [ ] `Contracts.Stage1to2`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage1 implementation project that:
  - [ ] Validates and normalizes mesh invariants required downstream
  - [ ] Produces deterministic ordering/canonical IDs as required by contract
  - [ ] Rejects unsupported/ambiguous degeneracies explicitly
- [ ] Orchestrator wiring for Stage1 with strict pre/post validation

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage1 tests demonstrate deterministic output for identical inputs

