# CDT Robustness TODO List

## 1. PolygonTriangulator2D (ring + ear clipping)

- [x] Add geometric self-intersection check for input rings  
- [ ] Document and unify epsilon/degeneracy policy  
- [ ] Distinguish ear-clipping failures (non-simple vs numeric)  
- [ ] Handle near-collinear ears explicitly  
- [ ] Factor ring validation into a helper method  

## 2. ConstraintEnforcer2D – segment/triangle intersection

- [ ] Split strict vs inclusive intersection logic  
- [ ] Detect and special-case collinear edge chains  
- [ ] Special‑case endpoints on edges/vertices  
- [ ] Document epsilons used  

## 3. ConstraintEnforcer2D – corridor construction

- [ ] Check corridor connectivity via BFS/DFS  
- [ ] Order corridor triangles by parametric t along AB  
- [ ] Validate intersection type for each triangle  
- [ ] Improve “no corridor found” diagnostics  

## 4. ConstraintEnforcer2D – boundary edges and cycles

- [ ] Validate boundary edge classification  
- [ ] Detect multiple boundary cycles  
- [ ] Detect duplicate vertices in cycles  
- [ ] Check geometric self‑intersection of the ring  
- [ ] Ensure AB appears exactly once  
- [ ] Consider hole support (future)  

## 5. Retrangulation + insertion

- [ ] Validate N−2 triangles returned  
- [ ] Replace corridor triangles cleanly  
- [ ] Enforce mesh invariants (area, manifold edges)  
- [ ] Verify constrained segment AB is present post‑insert  
- [ ] Track constrained edges globally  

## 6. Delaunay2DTriangulator global validation

- [ ] Add optional final validation pass  
- [ ] Expose validation via API flag  
- [ ] Document constraint semantics  

## 7. Tests + fuzzing

- [ ] Tests for self‑intersecting ring  
- [ ] Tests for collinear/thin polygons  
- [ ] Tests for hull‑edge constraints  
- [ ] Tests for endpoint‑on‑edge constraints  
- [ ] Tests for inner loops (expect fail for now)  
- [ ] Tests for crossing constraints (expect fail)  
- [ ] Random fuzz: non‑crossing constraints  
- [ ] Track failing seeds as regression tests  

## 8. Diagnostics

- [ ] Corridor debug dump facility  
- [ ] Standardize exception messages  
- [ ] Add debug flags  
- [ ] Include coordinates/indices in error messages  

## 9. Structural clean‑up

- [ ] Introduce MeshTopology2D abstraction  
- [ ] Extract CorridorBuilder  
- [ ] Keep PolygonTriangulator2D pure  
- [ ] Add ConstraintApplier layer  
