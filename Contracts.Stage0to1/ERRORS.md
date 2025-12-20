# Contracts.Stage0to1 — Error Codes

## Request validation

- `BP01.REQUEST.LEFT_MESH_ID_INVALID`
  - `leftMesh` missing or `leftMesh.id` empty after trimming.
- `BP01.REQUEST.RIGHT_MESH_ID_INVALID`
  - `rightMesh` missing or `rightMesh.id` empty after trimming.
- `BP01.REQUEST.OP_INVALID`
  - `op` is not a defined enum constant.
- `BP01.REQUEST.DETERMINISM_POLICY_INVALID`
  - `determinismPolicy` is not a defined enum constant.
- `BP01.REQUEST.COORDINATE_SPACE_INVALID`
  - `coordinateSpace` is not a defined enum constant.
- `BP01.REQUEST.TOLERANCES_MISSING`
  - `tolerances` is missing.

## Context (output) validation

- `BP01.CONTEXT.LEFT_MESH_ID_NOT_CANONICAL`
  - `leftMesh` missing or `leftMesh.id` empty, or has leading/trailing whitespace.
- `BP01.CONTEXT.RIGHT_MESH_ID_NOT_CANONICAL`
  - `rightMesh` missing or `rightMesh.id` empty, or has leading/trailing whitespace.
- `BP01.CONTEXT.OP_INVALID`
  - `op` is not a defined enum constant.
- `BP01.CONTEXT.DETERMINISM_POLICY_INVALID`
  - `determinismPolicy` is not a defined enum constant.
- `BP01.CONTEXT.COORDINATE_SPACE_INVALID`
  - `coordinateSpace` is not a defined enum constant.
- `BP01.CONTEXT.TOLERANCES_MISSING`
  - `tolerances` is missing.

## Core tolerance validation

See `Contracts.Core` error codes for tolerance validation failures:
- `CORE.TOLERANCE.DISTANCE_EPSILON_INVALID`
- `CORE.TOLERANCE.ANGLE_EPSILON_RADIANS_INVALID`
- `CORE.TOLERANCE.AREA_EPSILON_INVALID`
