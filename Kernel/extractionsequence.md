# Kernel Extraction Sequence

Goal: split Kernel into independent libraries in small, buildable steps.

- [x] Step 01: Extract `TriangleSubdivision` (project + `Kernel/TriangleSubdivision/*`).
- [x] Step 02: Extract `Intersection.Pair` (`IntersectionSet`, `PairFeatures`, `BarycentricVertices`).
- [x] Step 03: Extract `Intersection.Graph` (`IntersectionGraph`, `Intersection.Graph.Index` for `TriangleIntersectionIndex`, `Mesh*Topology`, `IntersectionCurve*`).
- [ ] Step 04: Extract `Patching` (`TrianglePatchSet`).
- [ ] Step 05: Extract `Classification` (`PatchClassifier`, `PointInMeshTester`, `RayIntersectsTriangle`).
- [ ] Step 06: Extract `Selection` (`BooleanPatchSet`, `BooleanPatchClassifier`, `BooleanOperation`).
- [ ] Step 07: Extract `Assembly` (`Kernel/BooleanAssembly/*`).
- [ ] Step 08: Extract `MeshInterop` (`BooleanMeshConverter`).
- [ ] Step 09: Keep `BooleanOps` as the thin API facade and wire new refs.

# Future goal

BooleanOps.Run(leftExternal, rightExternal, op)
  leftInternal  = MeshInterop.ToInternal(leftExternal)
  rightInternal = MeshInterop.ToInternal(rightExternal)

  idx        = Intersection.Graph/Index.Run(leftInternal, rightInternal)    (or separate Index step if you keep it)
  pairs      = Intersection.Pair.Run(leftInternal, rightInternal, idx)
  graph      = Intersection.Graph.Run(leftInternal, rightInternal, pairs)
  patches    = Patching.Run(leftInternal, rightInternal, graph)   // uses TriangleSubdivision
  classified = Classification.Run(leftInternal, rightInternal, patches)
  selected   = Selection.Run(op, classified)
  resultInt  = Assembly.Run(selected)

  resultExt  = MeshInterop.ToExternal(resultInt)
  return resultExt
