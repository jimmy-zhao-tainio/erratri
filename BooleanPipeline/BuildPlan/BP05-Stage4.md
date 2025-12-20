# BP05 — Stage4 (intersection graph) + `Contracts.Stage4to5`

## Goal
Lift intersection evidence into a global intersection graph model consumed by the Stage5 face-cutting pipeline.

## Owns
- `Contracts.Stage4to5`

## Depends on
- BP00
- BP04 (for `Contracts.Stage3to4`)

## Deliverables
- [ ] `Contracts.Stage4to5`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage4 implementation project that:
  - [ ] Builds a deterministic, globally indexed graph model (IDs + ordering per contract)
  - [ ] Emits only `Contracts.Stage4to5` DTOs
- [ ] Orchestrator wiring for Stage4 with strict pre/post validation

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage4 tests cover determinism and ID stability claims

