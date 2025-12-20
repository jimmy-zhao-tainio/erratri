# BP14 — Stage7 (assemble boolean mesh) + `Contracts.Stage7to8`

## Goal
Assemble classified patches into a boolean result mesh under explicit welding/manifold policies.

## Owns
- `Contracts.Stage7to8`

## Depends on
- BP00
- BP13 (for `Contracts.Stage6to7`)

## Deliverables
- [ ] `Contracts.Stage7to8`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage7 implementation project that:
  - [ ] Assembles output mesh deterministically (ordering + IDs per contract)
  - [ ] Applies explicit welding/manifold requirements; rejects unsupported topologies

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage7 tests cover representative assembly + failure modes

