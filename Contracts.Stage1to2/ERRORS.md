# Contracts.Stage1to2 — Error Codes

## Context (output) validation

- `BP02.CONTEXT.LEFT_MESH_ID_NOT_CANONICAL`
  - `leftMesh` missing or `leftMesh.id` empty, or has leading/trailing whitespace.
- `BP02.CONTEXT.RIGHT_MESH_ID_NOT_CANONICAL`
  - `rightMesh` missing or `rightMesh.id` empty, or has leading/trailing whitespace.
- `BP02.CONTEXT.OP_INVALID`
  - `op` is not a defined enum constant.
- `BP02.CONTEXT.DETERMINISM_POLICY_INVALID`
  - `determinismPolicy` is not a defined enum constant.
- `BP02.CONTEXT.COORDINATE_SPACE_INVALID`
  - `coordinateSpace` is not a defined enum constant.
- `BP02.CONTEXT.TOLERANCES_MISSING`
  - `tolerances` is missing.

## Core tolerance validation

See `Contracts.Core` error codes for tolerance validation failures:
- `CORE.TOLERANCE.DISTANCE_EPSILON_INVALID`
- `CORE.TOLERANCE.ANGLE_EPSILON_RADIANS_INVALID`
- `CORE.TOLERANCE.AREA_EPSILON_INVALID`

