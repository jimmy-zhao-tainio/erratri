# Contracts.Stage1to2 — Stage1 → Stage2 Contract

## Purpose
Normalize the Stage0 context into a Stage1 output suitable for intersection indexing.

## Input
Stage1 consumes `Contracts.Stage0to1.Stage0To1Context` and must validate it strictly using the Stage0 contract.

## Output: `Stage1To2Context`

### Fields
- `leftMesh.id` — required; non-empty, no leading/trailing whitespace.
- `rightMesh.id` — required; non-empty, no leading/trailing whitespace.
- `op` — valid enum value.
- `tolerances` — required; validated by `Contracts.Core.ToleranceBundle.ValidateStrict`.
- `coordinateSpace` — valid enum value.
- `determinismPolicy` — valid enum value.

### Output.ValidateStrict checks
1.1 `leftMesh` present; `leftMesh.id` not null/empty, and is canonical (trimmed).
1.2 `rightMesh` present; `rightMesh.id` not null/empty, and is canonical (trimmed).
1.3 `op` is a defined enum constant.
1.4 `determinismPolicy` is a defined enum constant.
1.5 `coordinateSpace` is a defined enum constant.
1.6 `tolerances` present and passes `ToleranceBundle.ValidateStrict`.

## Determinism and stability
- Stage1 output is deterministic for identical inputs.
- Canonicalization is exactly `Trim()` on mesh ids only; no case folding, Unicode normalization, or internal whitespace rewriting is permitted.

## Non-goals
- No mesh geometry processing or topology validation in Stage1.
- No policy inference or defaults; required fields must be explicit.

## Failure modes
All failures return stable error codes listed in `ERRORS.md`. Any undocumented case is a bug.

