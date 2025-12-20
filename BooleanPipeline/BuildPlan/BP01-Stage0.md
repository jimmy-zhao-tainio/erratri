# BP01 — Stage0 (entry normalization) + `Contracts.Stage0to1`

## Goal
Normalize the public boolean-op request into a canonical, deterministic Stage0→Stage1 context.

## Owns
- `Contracts.Stage0to1`

## Depends on
- BP00

## Deliverables
- [x] `Contracts.Stage0to1`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [x] Stage0 implementation project that:
  - [x] Normalizes input meshes/policies/tolerances into canonical form
  - [x] Fails fast on unsupported request shapes/cases (stable error codes)
  - [x] Emits only `Contracts.Stage0to1` DTOs
- [x] Orchestrator wiring for Stage0 with strict pre/post validation

## Done when
- [x] Contract conformance tests pass (good/bad + goldens)
- [x] Stage0 unit/integration tests prove strict pre/post validation and stable error codes
