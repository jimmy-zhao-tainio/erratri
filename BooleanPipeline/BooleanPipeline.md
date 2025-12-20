# BooleanPipeline (Contract-First Boolean Mesh Pipeline)

BooleanPipeline is a clean-reset, contract-first boolean ops kernel implemented as a pipeline of stages.

`Kernel` is ignored at runtime. Code may be copied from `Kernel` as raw material; once copied, it is owned by BooleanPipeline and evolves under BooleanPipeline rules.

## Goals

- Make stage boundaries explicit, deterministic, and mechanically enforced.
- Prevent “LLM drift” by forcing shared language: the same DTOs, the same docs, the same validators.
- Fail fast on unsupported cases (no silent degradation).

## Non-goals (initially)

- Performance optimization.
- “Handle everything” geometry; unsupported cases must be enumerated and rejected with stable error codes.

## Core structure

### Contract-per-boundary (spine)

For every adjacency (stage↔stage, substage↔substage), there is exactly one contract project:

- `Contracts.Stage0to1`, `Contracts.Stage1to2`, …, `Contracts.Stage7to8`

Each contract project contains:

- Boundary DTO types (inputs/outputs that both sides talk about)
- Fully implemented strict validators for those DTOs (pre/post)
- Canonical `CONTRACT.md` + `ERRORS.md`
- Conformance tests + golden examples (with normalization rules)

All boundary contracts depend on `Contracts.Core`, which defines shared vocabulary and enforcement utilities (spaces/units, tolerance bundles, determinism rules, ID model helpers, error taxonomy base, serialization normalization).

### Stages

Each stage is an implementation project that references only:

- Its input contract(s)
- Its output contract(s)
- Its internal helpers

A stage must not define boundary structures of its own.

### Mandatory rule: validators are not optional

Every stage/substage must:

- Validate Input strictly before doing any work.
- Validate Output strictly immediately before returning.
- Implement validators to match the contract text. If the contract does not mention a case, it must be rejected with a stable error code.

## Stage map (high level)

- **Stage0**: Boolean op entrypoint (normalize inputs and policies)
- **Stage1**: Preprocess meshes (cleanup/validation/normalization)
- **Stage2**: Build intersection index (spatial acceleration + topology lookup inputs)
- **Stage3**: Compute pairwise intersections (evidence generation)
- **Stage4**: Build intersection graph (global vertices/edges + pairing model)
- **Stage5**: Face cutting sub-pipeline (constraints → planar embedding → regions → triangles → lifted patches)
- **Stage6**: Classify patches (A/B keep/discard semantics)
- **Stage7**: Assemble boolean mesh (indexing + welding policy + manifold requirements)
- **Stage8**: Postprocess + validate (final cleanup + invariants)

### Stage5 sub-stages

Each Stage5 sub-stage is a full stage in the same sense: its own library, its own boundary DTOs, its own strict validators, and its own contract docs.

Internal contract projects exist for each Stage5 adjacency:

- `Contracts.Stage5aTo5b`, `Contracts.Stage5bTo5c`, `Contracts.Stage5cTo5d`, `Contracts.Stage5dTo5e`, `Contracts.Stage5eTo5f`
and Stage5 boundary contracts:

- `Contracts.Stage4to5` (Stage4 → Stage5 entry)
- `Contracts.Stage5to6` (Stage5 exit → Stage6)

Stage5 sub-stages:

- **Stage5a** Collect per-face constraints (3D constraints)
- **Stage5b** Embed constraints to 2D (planar embedding for face)
- **Stage5c** Build PSLG (planar straight-line graph)
- **Stage5d** Select keep regions (boolean semantics on faces)
- **Stage5e** Triangulate selected regions (using the existing constrained triangulator)
- **Stage5f** Lift triangles to 3D and emit patches

## Cross-cutting contract invariants (mandatory)

Every `CONTRACT.md` must declare (and validators must enforce):

- **Coordinate space and units** (world vs. face-local 2D vs. mesh-local, plus unit assumptions)
- **Orientation and winding** (triangle winding, “inside” definition)
- **Tolerance sources** (all epsilons come from an explicit tolerance bundle carried in the input)
- **Determinism guarantees** (ordering rules, canonical iteration, ID stability claims)
- **ID model** (stable vs. ephemeral IDs, assignment rules, mapping when IDs are reassigned)
- **Allowed degeneracies** (explicitly allowed vs. hard failures)
- **Unsupported cases** (enumerated with error codes; everything else is a bug)
- **Serialization normalization** (quantization/rounding + ordering + formatting for golden examples)
- **Error taxonomy** (stable error codes; no ad-hoc exception text)

## “Docs are the contract” enforcement

- Contracts are single-source: adjacent stages do not copy wording; they depend on the same contract project.
- Validators and conformance tests must exist and must pass before a stage/substage is considered valid.
- CI requires contract conformance tests to pass before any stage can be considered valid.

## External dependencies (where they live)

- **PSLG** is a Stage5 internal tool (typically in Stage5c), and only appears behind Stage5 contract DTOs.
- **ConstrainedTriangulator** is used in Stage5e and is governed by the Stage5e contract.

## Build plan

Framework first, then one build row per stage/substage. Each row has a dedicated plan file (checklist + acceptance criteria).

A row is considered **done** only when:

- The owned contract project exists (DTOs + strict validators + `CONTRACT.md` + `ERRORS.md` + conformance tests + goldens).
- The stage/substage implementation calls strict validation pre/post and rejects all undocumented cases with stable error codes.
- The orchestrator can wire it without introducing cross-boundary types.

| Row | Scope | Owns contract(s) | Plan |
| --- | --- | --- | --- |
| BP00 | Framework + scaffolding | `Contracts.Core` | [BP00-Framework](BuildPlan/BP00-Framework.md) |
| BP01 | Stage0: entry normalization | `Contracts.Stage0to1` | [BP01-Stage0](BuildPlan/BP01-Stage0.md) |
| BP02 | Stage1: preprocess meshes | `Contracts.Stage1to2` | [BP02-Stage1](BuildPlan/BP02-Stage1.md) |
| BP03 | Stage2: build intersection index | `Contracts.Stage2to3` | [BP03-Stage2](BuildPlan/BP03-Stage2.md) |
| BP04 | Stage3: compute intersections | `Contracts.Stage3to4` | [BP04-Stage3](BuildPlan/BP04-Stage3.md) |
| BP05 | Stage4: build intersection graph | `Contracts.Stage4to5` | [BP05-Stage4](BuildPlan/BP05-Stage4.md) |
| BP06 | Stage5: face cutting pipeline (orchestrate 5a–5f) | `Contracts.Stage5to6` | [BP06-Stage5](BuildPlan/BP06-Stage5.md) |
| BP07 | Stage5a: collect per-face constraints | `Contracts.Stage5aTo5b` | [BP07-Stage5a](BuildPlan/BP07-Stage5a.md) |
| BP08 | Stage5b: embed constraints to 2D | `Contracts.Stage5bTo5c` | [BP08-Stage5b](BuildPlan/BP08-Stage5b.md) |
| BP09 | Stage5c: build PSLG | `Contracts.Stage5cTo5d` | [BP09-Stage5c](BuildPlan/BP09-Stage5c.md) |
| BP10 | Stage5d: select keep regions | `Contracts.Stage5dTo5e` | [BP10-Stage5d](BuildPlan/BP10-Stage5d.md) |
| BP11 | Stage5e: constrained triangulation | `Contracts.Stage5eTo5f` | [BP11-Stage5e](BuildPlan/BP11-Stage5e.md) |
| BP12 | Stage5f: lift triangles + emit patches | — | [BP12-Stage5f](BuildPlan/BP12-Stage5f.md) |
| BP13 | Stage6: classify patches | `Contracts.Stage6to7` | [BP13-Stage6](BuildPlan/BP13-Stage6.md) |
| BP14 | Stage7: assemble boolean mesh | `Contracts.Stage7to8` | [BP14-Stage7](BuildPlan/BP14-Stage7.md) |
| BP15 | Stage8: postprocess + validate | — | [BP15-Stage8](BuildPlan/BP15-Stage8.md) |

Rule: a stage cannot “exist” without its boundary contracts, validators, and conformance tests.

