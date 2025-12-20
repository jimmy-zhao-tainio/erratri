# BP11 — Stage5e (constrained triangulation) + `Contracts.Stage5eTo5f`

## Goal
Triangulate selected planar regions and emit a triangulation representation suitable for 3D lifting.

## Owns
- `Contracts.Stage5eTo5f`

## Depends on
- BP00
- BP10 (for `Contracts.Stage5dTo5e`)

## Deliverables
- [ ] `Contracts.Stage5eTo5f`: DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens
- [ ] Stage5e implementation project that:
  - [ ] Uses the existing constrained triangulator internally
  - [ ] Declares (and enforces) the Steiner-point policy in the contract (forbid, or represent + lift deterministically)
  - [ ] Emits only contract DTOs to Stage5f

## Done when
- [ ] Contract conformance tests pass (good/bad + goldens)
- [ ] Stage5e tests cover triangulation determinism and policy enforcement

