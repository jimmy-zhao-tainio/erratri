# BP15 — Stage8 (postprocess + validate)

## Goal
Finalize the output mesh: cleanup, enforce invariants, and return a validated result.

## Owns
- (none; final pipeline output definition lives with the entry/output API shape)

## Depends on
- BP00
- BP14 (for `Contracts.Stage7to8`)

## Deliverables
- [ ] Stage8 implementation project that:
  - [ ] Consumes `Contracts.Stage7to8` DTOs
  - [ ] Performs final cleanup and invariant enforcement
  - [ ] Produces a deterministic, validated result (error codes for all unsupported outcomes)
- [ ] End-to-end pipeline goldens for representative boolean cases (as soon as upstream stages exist)

## Done when
- [ ] Stage8 tests cover postprocess invariants + stable failures

