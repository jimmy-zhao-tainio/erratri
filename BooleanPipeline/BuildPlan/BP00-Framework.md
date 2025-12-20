# BP00 — Framework & scaffolding

## Goal
Create the shared contract foundation and enforcement harness that every stage/substage must use.

## Owns
- `Contracts.Core`

## Deliverables
- [ ] `Contracts.Core` vocabulary: coordinate spaces/units, tolerance bundles, determinism rules, ID model helpers
- [ ] `Contracts.Core` enforcement utilities: strict validation primitives + base error taxonomy + error code model
- [ ] `Contracts.Core` golden normalization utilities (quantization/rounding + ordering + formatting)
- [ ] Contract conformance test harness (shared helpers for “good”/“bad” DTO validation + golden stability)
- [ ] Composition-only orchestrator/pipeline runner skeleton (wiring only; no geometry logic)
- [ ] Stage boundary guard helper enforcing: validate input before work; validate output before return

## Done when
- [ ] A sample contract project can reference `Contracts.Core` and run conformance tests with stable goldens
- [ ] Orchestrator can run a no-op pipeline while enforcing boundary validation calls

