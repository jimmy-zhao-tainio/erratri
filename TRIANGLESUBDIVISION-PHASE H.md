We are in Kernel/PslgCore.cs and need to make PSLG faces hole-aware.

Current problem (repro from Demo.Intersections tetra peek example):

For one triangle on mesh A we get three interior intersection points and three segments forming a closed tiny triangle completely inside the base triangle. PSLG produces raw cycles: the outer triangle (0,1,2) and its reverse (1,0,2), and the inner tiny triangle (3,5,4) and its reverse (5,3,4). Interior selection plus triangulation treat “outer 0,1,2” and “inner 3,5,4” as independent faces and triangulate both, so the inner region is double-counted. Result: original area = 8, patch sum = 12 for that triangle.

This geometry is valid: the intersection loop is a closed curve wholly inside the triangle, so topologically the domain is “outer triangle MINUS inner triangle”. PSLG must support faces with holes rather than modelling this as two separate full faces.

Goal:

Extend PSLG so that a PslgFace can represent one outer boundary cycle and zero or more hole cycles. The face’s signed area in UV should be area(outer) minus the sum of the areas of the holes. Triangulation of a face with holes must produce triangles that cover exactly the ring region (outer minus holes) without double-counting. The tetra peek example’s A-triangle must become correct: patch sum approximately equal to original area, not original plus inner. Work only in Kernel/PslgCore.cs (and adjust Kernel.Tests or Demo.Intersections as needed). Do not change the public API of TriangleSubdivision.Subdivide.

Step 1: Upgrade PslgFace to support holes.

Replace the existing PslgFace definition with something like:

    internal sealed class PslgFace
    {
        public int[] OuterVertices { get; }
        public IReadOnlyList<int[]> Holes { get; }
        public double SignedAreaUV { get; } // outer - sum(holes), in UV chart
    }

OuterVertices and each hole cycle should be CCW in UV, meaning they have positive signed area when measured in the UV chart using Geometry2D. Update any code that used PslgFace.Vertices or PslgFace.SignedArea to use OuterVertices and SignedAreaUV instead.

Step 2: Keep building raw cycles as now.

Keep the current half-edge structure and BuildHalfEdges logic. Each undirected PSLG edge still has two half-edges (twins). Next for each half-edge is set using the “CCW successor of the twin at the destination” rule. ExtractFaces must still walk each unvisited half-edge following Next until it returns to the start, produce a raw cycle int[] of vertex indices, compute its signed area in UV (which may be positive or negative at this stage), and store a sample UV point that is guaranteed to lie inside the cycle (for example the centroid of the UV positions of its vertices). These are raw cycles only; they are not yet faces-with-holes.

Step 3: Build a nesting tree of cycles and construct PslgFace instances.

Introduce an internal RawCycle class or struct holding: Vertices (int[]), SignedAreaUV (double), SamplePointUV ((double X, double Y)), and Index.

After ExtractFaces collects all cycles, normalise each cycle so that if SignedAreaUV is negative you reverse the vertex order and flip the sign so SignedAreaUV becomes positive. Compute SamplePointUV as the centroid of the UV positions of its vertices.

Next, build nesting relations. For each RawCycle ci, test its SamplePointUV against every other RawCycle cj that has larger SignedAreaUV. Use an existing Geometry2D point-in-polygon predicate in UV (strict inside, not on edge) to determine whether ci lies inside cj. If ci lies inside several cycles, choose as its parent the container with the smallest area. This yields a forest of cycles with parent pointers.

For each root cycle (a cycle with no parent), perform a depth-first traversal of its subtree. Cycles at even depth (0, 2, 4, …) are considered “solid”; cycles at odd depth (1, 3, …) are holes relative to that root. For each root, construct one PslgFace as follows: OuterVertices is the root’s Vertices; Holes contains copies of the Vertices of all descendant cycles at odd depth; SignedAreaUV equals root.SignedAreaUV minus the sum of SignedAreaUV for all hole cycles.

Discard any face whose absolute SignedAreaUV is below Tolerances.EpsArea. Ensure there is no duplication: two different PslgFace instances must not share the same outer boundary. You can reuse the existing canonical rotation-invariant key logic you already wrote (find smallest vertex id, rotate cycle, compare sequences) to ensure each distinct outer cycle appears only once.

At this point, the tetra peek triangle on A should yield one PslgFace whose OuterVertices are the big triangle (indices 0,1,2) and whose Holes contains one cycle with the tiny inner triangle (indices 3,4,5).

Step 4: Triangulate faces with holes.

Extend the triangulation logic that currently works on a single-cycle PslgFace so it can also handle holes.

For faces where Holes.Count is zero, keep the current ear-clipping pipeline on OuterVertices exactly as it is (in UV). For faces where Holes.Count is greater than zero, implement a simple bridge-and-ear-clip algorithm:

For each hole H in the face, pick a vertex h on H (for example the one with smallest X, then Y in UV). Find a vertex o on the outer boundary such that the segment h–o, in UV, does not cross any PSLG edge and does not pass through the interior of any other hole. Use the existing RealSegment/Geometry2D segment-intersection predicates in UV for this visibility test. Add a bridge edge h–o in a working copy of the polygon edge set.

After bridges have been added for all holes, construct a single stitched simple polygon that represents the outer boundary plus the holes connected by the bridges. You may use standard polygon-with-holes stitching: walk around the outer boundary, and when a bridge is encountered, follow the bridge into the hole cycle, traverse the hole, then follow the bridge back, and continue around the outer boundary. The result should be a simple, non-self-intersecting polygon in UV.

Run the existing ear-clipping triangulator on this stitched polygon in UV to produce triangles. For each PslgFace, compute sumUV as the sum of signed UV triangle areas for all triangles associated with that face. Enforce a per-face invariant:

    Math.Abs(sumUV - face.SignedAreaUV) <= Tolerances.EpsArea * k

for some small k (for example 10). If this fails, throw a descriptive exception including the outer and hole vertex indices so we can debug.

Map each UV triangle to a RealTriangle patch in world space exactly as currently done for simple faces.

Step 5: Integrate with SelectInteriorFaces and callers.

Update SelectInteriorFaces so it operates on a list of PslgFace (each with outer and optional holes). The outer or unbounded face, if present, is still identified as the one with the largest absolute SignedAreaUV and is excluded from the interior set. All remaining faces with SignedAreaUV greater than Tolerances.EpsArea are treated as interior faces and triangulated.

Ensure that callers, including TriangleSubdivision.Subdivide and relevant tests, now treat PslgFace as “outer with optional holes”. Do not change the public signature of TriangleSubdivision.Subdivide.

Step 6: Verification.

All existing unit tests in Kernel.Tests must still pass (update them only where they touched the old PslgFace structure). In Demo.Intersections, the tetra peek example must change from

    A0: points=3, segments=3, patches=3, area orig=8, patches=12, diff=4, ok=False

to something where the A0 line reports patch area approximately equal to the original triangle area (8) and ok=True for that triangle. The intersection triangles on B must remain correct.

Finally, run the fuzz harness:

    dotnet run --project TriangleSubdivision.Fuzz -- 100000 8 12345

The previous “iteration 1” failure due to full-triangle plus inner-triangle double-count must no longer occur. If fuzz finds new failing configurations, keep the logging and report them as usual.
