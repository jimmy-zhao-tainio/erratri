# BP09 — Stage5c (build PSLG) + `Contracts.Stage5cTo5d`

## Goal
Convert embedded constraints into a PSLG representation suitable for region selection and triangulation.

## Owns
- `Contracts.Stage5cTo5d`

## Depends on
- BP00
- BP08 (for `Contracts.Stage5bTo5c`)

## Deliverables
- [ ] `Contracts.Stage5cTo5d`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage5c implementation project that:
  - [ ] Uses PSLG internally, but exposes only contract DTOs across the boundary
  - [ ] Produces deterministic vertex/edge ordering as required by the contract

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage5c tests confirm no PSLG types leak across boundary

