# CDT Robustness TODO List (v3)

## 1. PolygonTriangulator2D (ring + ear clipping)

- [x] Add geometric self-intersection check for input rings  
- [x] Document and unify epsilon/degeneracy policy  
- [x] Distinguish ear-clipping failures (non-simple vs numeric)  
- [x] Handle near-collinear ears explicitly  
- [x] Factor ring validation into a helper method  

## 2. ConstraintEnforcer2D – segment/triangle intersection

- [x] Document strict vs inclusive intersection semantics at the *constraint* level  
- [x] Detect and special-case collinear edge chains (pure collinear ⇒ explicit failure)  
- [x] Special-case endpoints on edges/vertices (EndpointInside vs TouchEdge/TouchVertex vs “edge exists”)  
- [x] Document epsilons used and when “touching” counts as intersecting  

## 3. ConstraintEnforcer2D – corridor construction

- [x] Check corridor connectivity via BFS/DFS (EnsureCorridorIsSingleComponent)  
- [x] (Revisit / replace) “Order corridor triangles by parametric t along AB” – superseded by boundary ring validation; no parametric ordering needed  
- [x] Validate intersection type for each corridor triangle via invariants (we classify, but we don’t assert “corridor only contains interior/endpoint/collinear” anywhere yet)  
- [x] Improve “no corridor found” diagnostics (include indices / coords / intersection classification)  

## 4. ConstraintEnforcer2D – boundary edges and cycles

- [x] Validate boundary edge classification via edge-count + degree-2 checks  
- [x] Detect multiple boundary cycles (visitedEdges/visitedVertices checks)  
- [x] Detect duplicate vertices in cycles  
- [x] Check geometric self-intersection of the boundary ring (ring-level HasSelfIntersectionProper)  
- [x] Ensure AB appears exactly once in the boundary ring  
- [x] Implement hole support for corridor patches (outer + inner rings via TriangulateWithHoles)  

## 5. Re-triangulation + insertion

- [x] Validate N−2 triangles returned for each corridor ring (sanity check around PolygonTriangulator2D)  
- [x] Replace corridor triangles cleanly (no leaked/duplicated triangles)  
- [x] Enforce mesh invariants locally after insert (positive area, manifold edges)  
- [x] Verify constrained segment AB is present post-insert (EnsureConstrainedEdgePresent)  
- [x] Track constrained edges globally (for later validation and debugging)  
- [ ] 5.5 Optional local Delaunay relax pass post-corridor (opt-in, re-impose Delaunay in the AB patch without changing defaults)  

## 6. Delaunay2DTriangulator global validation

- [x] Add optional final validation pass (mesh + constraints)  
- [x] Expose validation via API flag / debug mode  
- [x] Document constraint semantics (what the API guarantees)  

## 7. Tests + fuzzing

- [x] Tests for self-intersecting ring (degenerate and non-degenerate)  
- [x] Tests for collinear/thin polygons (very skinny but valid)  
- [x] Tests for hull-edge constraints (we now have a hull-edge constraint test in DelaunayTriangulatorTests)  
- [x] Tests for endpoint-on-edge constraints (endpoint exactly on an existing edge, not just at a vertex)  
- [x] Tests for corridor boundaries with inner loops (outer ring + inner ring) – now hole-aware, passing  
- [x] Tests for crossing constraints (expect fail)  
- [ ] Random fuzz: non-crossing constraints  
- [ ] Track failing seeds as regression tests  

(Plus: we now have a dedicated classifier test:
`Classifier_Distinguishes_CollinearOverlap_And_ProperInterior`, which effectively nails the 2.x semantics.)  

## 8. Diagnostics

- [x] Corridor debug dump facility (triangles, boundary ring, AB, intersection types)  
- [ ] Standardize exception messages (clear categories: input, corridor, numeric)  
- [ ] Add debug flags / switches to enable extra checks and dumps  
- [ ] Include coordinates/indices in error messages where useful  

## 9. Structural clean-up

- [x] Introduce MeshTopology2D abstraction (edges ? triangles)  
- [x] Extract CorridorBuilder from ConstraintEnforcer2D  
- [x] Keep PolygonTriangulator2D pure (no constraint-specific behaviour)  
- [x] Add ConstraintApplier layer over Delaunay2DTriangulator






