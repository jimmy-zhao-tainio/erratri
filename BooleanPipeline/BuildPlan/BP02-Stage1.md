# BP02 — Stage1 (preprocess meshes) + `Contracts.Stage1to2`

## Goal
Take the Stage0 context and produce normalized, validated meshes suitable for intersection work.

## Owns
- `Contracts.Stage1to2`

## Depends on
- BP00
- BP01 (for `Contracts.Stage0to1`)

## Deliverables
- [x] `Contracts.Stage1to2`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [x] Stage1 implementation project that:
  - [x] Validates and normalizes mesh invariants required downstream
  - [x] Produces deterministic ordering/canonical IDs as required by contract
  - [x] Rejects unsupported/ambiguous degeneracies explicitly
- [x] Orchestrator wiring for Stage1 with strict pre/post validation

## Done when
- [x] Contract conformance tests pass (good/bad + goldens)
- [x] Stage1 tests demonstrate deterministic output for identical inputs
