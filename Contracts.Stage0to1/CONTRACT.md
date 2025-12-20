# Contracts.Stage0to1 — Stage0 → Stage1 Contract

## Purpose
Normalize a public boolean-op request into a canonical, deterministic context for Stage1.

## Input: `BooleanOpRequest`

### Fields
- `leftMesh.id` — required; non-empty after trimming.
- `rightMesh.id` — required; non-empty after trimming.
- `op` — required; must be a supported boolean operation.
- `tolerances` — required; validated by `Contracts.Core.ToleranceBundle.ValidateStrict`.
- `coordinateSpace` — required; must be a defined `Contracts.Core.CoordinateSpace`.
- `determinismPolicy` — required; must be a defined `Contracts.Stage0to1.DeterminismPolicy`.

### Input.ValidateStrict checks
1.1 `leftMesh` present; `leftMesh.id` not null/empty after trimming.
1.2 `rightMesh` present; `rightMesh.id` not null/empty after trimming.
1.3 `op` is a defined enum constant.
1.4 `determinismPolicy` is a defined enum constant.
1.5 `coordinateSpace` is a defined enum constant.
1.6 `tolerances` present and passes `ToleranceBundle.ValidateStrict`.

## Output: `Stage0To1Context`

### Fields
Same as input, but canonicalized:
- `leftMesh.id` — required; non-empty, no leading/trailing whitespace.
- `rightMesh.id` — required; non-empty, no leading/trailing whitespace.
- `op` — valid enum value.
- `tolerances` — required; validated by `ToleranceBundle.ValidateStrict`.
- `coordinateSpace` — valid enum value.
- `determinismPolicy` — valid enum value.

### Output.ValidateStrict checks
2.1 `leftMesh` present; `leftMesh.id` not null/empty, and is canonical (trimmed).
2.2 `rightMesh` present; `rightMesh.id` not null/empty, and is canonical (trimmed).
2.3 `op` is a defined enum constant.
2.4 `determinismPolicy` is a defined enum constant.
2.5 `coordinateSpace` is a defined enum constant.
2.6 `tolerances` present and passes `ToleranceBundle.ValidateStrict`.

## Determinism and stability
- Stage0 output is deterministic for identical inputs.
- Canonicalization is exactly `Trim()` on mesh ids only; no case folding, Unicode normalization, or internal whitespace rewriting is permitted.

## Non-goals
- No mesh geometry processing or topology validation in Stage0.
- No policy inference or defaults; required fields must be explicit.

## Failure modes
All failures return stable error codes listed in `ERRORS.md`. Any undocumented case is a bug.
