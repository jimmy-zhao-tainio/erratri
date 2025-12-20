# BP06 — Stage5 (face cutting pipeline) + `Contracts.Stage5to6`

## Goal
Orchestrate Stage5a–Stage5f and emit the patch sets consumed by Stage6.

## Owns
- `Contracts.Stage5to6`

## Depends on
- BP00
- BP05 (for `Contracts.Stage4to5`)
- BP07–BP12 (for full Stage5 functionality)

## Deliverables
- [ ] `Contracts.Stage5to6`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage5 orchestrator implementation project that:
  - [ ] Runs 5a→5f in a deterministic order and wires their boundary DTOs only
  - [ ] Enforces strict validation at every internal boundary (5a→5b→…→5f)
  - [ ] Emits only `Contracts.Stage5to6` DTOs to Stage6
- [ ] End-to-end conformance-style tests for Stage5 using small canonical inputs

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage5 end-to-end tests run 5a–5f with strict validation at each boundary

