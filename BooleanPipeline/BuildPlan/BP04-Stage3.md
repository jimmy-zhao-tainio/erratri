# BP04 — Stage3 (pairwise intersections) + `Contracts.Stage3to4`

## Goal
Compute pairwise intersections and emit intersection “evidence” for graph construction.

## Owns
- `Contracts.Stage3to4`

## Depends on
- BP00
- BP03 (for `Contracts.Stage2to3`)

## Deliverables
- [ ] `Contracts.Stage3to4`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage3 implementation project that:
  - [ ] Computes intersection evidence in a deterministic order (as declared by the contract)
  - [ ] Explicitly rejects unsupported intersection configurations with stable error codes
- [ ] Orchestrator wiring for Stage3 with strict pre/post validation

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage3 tests cover determinism + representative unsupported cases

