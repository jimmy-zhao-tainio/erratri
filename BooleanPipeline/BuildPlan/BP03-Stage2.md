# BP03 — Stage2 (intersection index) + `Contracts.Stage2to3`

## Goal
Build the spatial/topological index that accelerates intersection queries and supports evidence generation.

## Owns
- `Contracts.Stage2to3`

## Depends on
- BP00
- BP02 (for `Contracts.Stage1to2`)

## Deliverables
- [ ] `Contracts.Stage2to3`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage2 implementation project that:
  - [ ] Builds deterministic acceleration/index structures (with canonical ordering rules)
  - [ ] Exposes only contract DTOs downstream (no leaking internal index types)
- [ ] Orchestrator wiring for Stage2 with strict pre/post validation

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage2 tests confirm deterministic indexing and stable IDs per contract

