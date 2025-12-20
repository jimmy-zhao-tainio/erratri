# BP10 — Stage5d (select keep regions) + `Contracts.Stage5dTo5e`

## Goal
Apply boolean semantics to select the kept planar regions per face.

## Owns
- `Contracts.Stage5dTo5e`

## Depends on
- BP00
- BP09 (for `Contracts.Stage5cTo5d`)

## Deliverables
- [ ] `Contracts.Stage5dTo5e`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage5d implementation project that:
  - [ ] Encodes boolean keep/discard rules deterministically
  - [ ] Rejects ambiguous classifications explicitly (stable codes)

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage5d tests cover representative classification cases

