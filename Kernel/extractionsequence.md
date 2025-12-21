# Kernel Extraction Sequence

Goal: split Kernel into independent libraries in small, buildable steps.

- [x] Step 01: Extract `Boolean.Triangulation` (project + `Kernel/TriangleSubdivision/*`).
- [x] Step 02: Extract `Boolean.Intersection.Pair` (`IntersectionSet`, `PairFeatures`, `BarycentricVertices`).
- [x] Step 03: Extract `Boolean.Intersection.Graph` (`IntersectionGraph`, `Boolean.Intersection.Graph.Index` for `TriangleIntersectionIndex`, `Mesh*Topology`, `IntersectionCurve*`).
- [x] Step 04: Extract `Boolean.Patching` (`TrianglePatchSet`).
- [x] Step 05: Extract `Boolean.Classification` (`PatchClassifier`, `PointInMeshTester`, `RayIntersectsTriangle`).
- [x] Step 06: Extract `Boolean.Selection` (`BooleanPatchSet`, `BooleanPatchClassifier`, `BooleanOperation`).
- [x] Step 07: Extract `Boolean.Assembly` (`Kernel/BooleanAssembly/*`).
- [x] Step 08: Extract `Boolean.MeshInterop` (`BooleanMeshConverter`).
- [ ] Step 09: Keep `BooleanOps` as the thin API facade and wire new refs.

# Future goal

BooleanOps.Run(leftExternal, rightExternal, op)
  leftInternal  = MeshInterop.ToInternal(leftExternal)
  rightInternal = MeshInterop.ToInternal(rightExternal)

  idx        = Boolean.Intersection.Graph/Index.Run(leftInternal, rightInternal)    (or separate Index step if you keep it)
  pairs      = Boolean.Intersection.Pair.Run(leftInternal, rightInternal, idx)
  graph      = Boolean.Intersection.Graph.Run(leftInternal, rightInternal, pairs)
  patches    = Boolean.Patching.Run(leftInternal, rightInternal, graph)   // uses Boolean.Triangulation
  classified = Boolean.Classification.Run(leftInternal, rightInternal, patches)
  selected   = Boolean.Selection.Run(op, classified)
  resultInt  = Boolean.Assembly.Run(selected)

  resultExt  = MeshInterop.ToExternal(resultInt)
  return resultExt
