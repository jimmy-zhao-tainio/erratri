# BP13 — Stage6 (classify patches) + `Contracts.Stage6to7`

## Goal
Classify emitted patches into keep/discard semantics for boolean assembly.

## Owns
- `Contracts.Stage6to7`

## Depends on
- BP00
- BP06 (for `Contracts.Stage5to6`)

## Deliverables
- [ ] `Contracts.Stage6to7`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage6 implementation project that:
  - [ ] Applies A/B classification rules deterministically
  - [ ] Produces only `Contracts.Stage6to7` DTOs for Stage7

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage6 tests cover representative classification cases

