1. Input model (TriangleGarden)

We work with exactly this:

Points: list of 2D coordinates.

Segments: list of existing edges, as index pairs into Points.

All “boundary edges” are just entries in Segments.
The triangulator never needs to distinguish boundary vs internal; it only knows:

“These segments may never be crossed by any triangle edge.”

Every time we create a triangle, all its edges get added to Segments and become new constraints.

2. Enforcer rules

Before we add any edge or triangle, we run it through two checks.

2.1 No crossing rule

An edge (u, v) is forbidden if the open segment between Points[u] and Points[v] crosses the interior of any existing segment in Segments (except when it shares an endpoint).

So: classic segment–segment intersection test.

2.2 Kindergarten neighbor rule

Let N(x) be the set of vertices that share a segment with vertex x (using the current Segments set).

For a new edge (u, v):

If N(u) is empty or N(v) is empty → edge is allowed.
(Early in construction, no structure yet.)

Otherwise, we require:

N(u) ∩ N(v) is not empty.

In words:

If both endpoints already have neighbors, they must share at least one neighbor.
If their neighbor worlds are completely disjoint, we are not allowed to suddenly connect them.

This kills the cursed “giant fan” diagonals like your B–C in the X-triangle picture.

We wrap this as:

IsEdgeAllowed(u, v, segments)

3. Triangle legality

A triangle (a, b, c) is legal iff:

All three edges (a,b), (b,c), (c,a) pass IsEdgeAllowed.

None of those edges crosses any existing segment.

The triangle has non-zero area (no collinear triple).

We write:

IsLegalTriangle(a, b, c, points, segments)


If this returns true, we’re allowed to add all missing edges of that triangle to Segments.

4. Global “Triangle Garden” algorithm (pseudo code)

This is the dumb global grower.
No corridor logic, no ear clipping up front, just:

keep trying triangles until you can’t add any more edges.

function TriangulateGlobally(points, segments, maxIterations):

    changed   = true
    iteration = 0

    while changed AND iteration < maxIterations:
        changed   = false
        iteration = iteration + 1

        n = number of points
        if n < 3:
            break

        // --- Pick two distinct random points p1, p2 ---
        p1 = randomInt(0, n-1)
        repeat:
            p2 = randomInt(0, n-1)
        until p2 != p1

        // === CASE 1: edge (p1, p2) already exists ===
        if ContainsEdge(segments, p1, p2):

            // Try to find ANY third point that makes a legal triangle
            for each index p3 in RandomPermutation(0..n-1):
                if p3 == p1 OR p3 == p2:
                    continue

                if IsLegalTriangle(p1, p2, p3, points, segments):
                    AddTriangleEdges(p1, p2, p3, segments)
                    changed = true
                    break // stop searching p3

            continue // go to next outer iteration


        // === CASE 2: edge (p1, p2) does NOT exist ===
        else:

            // Collect neighbors of p1 and p2 from current segments
            neighborSet = empty set
            for each (A,B) in segments:
                if A == p1: neighborSet.add(B)
                if B == p1: neighborSet.add(A)
                if A == p2: neighborSet.add(B)
                if B == p2: neighborSet.add(A)

            // Try forming triangles using these neighbors as p3
            for each p3 in RandomPermutation(neighborSet):
                if IsLegalTriangle(p1, p2, p3, points, segments):
                    AddTriangleEdges(p1, p2, p3, segments)
                    changed = true
                    break // stop searching p3

            continue // next outer iteration

    return segments

Helper routines (pseudo)
function ContainsEdge(segments, a, b):
    for each (A,B) in segments:
        if (A == a AND B == b) OR (A == b AND B == a):
            return true
    return false


function AddTriangleEdges(a, b, c, segments):
    if NOT ContainsEdge(segments, a, b):
        segments.add( (a,b) )
    if NOT ContainsEdge(segments, b, c):
        segments.add( (b,c) )
    if NOT ContainsEdge(segments, c, a):
        segments.add( (c,a) )

function IsLegalTriangle(a, b, c, points, segments):

    // 1. Edge-level kindergarten rule
    if NOT IsEdgeAllowed(a, b, segments): return false
    if NOT IsEdgeAllowed(b, c, segments): return false
    if NOT IsEdgeAllowed(c, a, segments): return false

    // 2. No segment crossing
    if EdgeCrossesExisting(a, b, points, segments): return false
    if EdgeCrossesExisting(b, c, points, segments): return false
    if EdgeCrossesExisting(c, a, points, segments): return false

    // 3. Non-degenerate area
    if TriangleArea(points[a], points[b], points[c]) == 0: return false

    return true

function IsEdgeAllowed(u, v, segments):

    if u == v: return false

    neighborsU = empty set
    neighborsV = empty set

    for each (A,B) in segments:
        if A == u: neighborsU.add(B)
        else if B == u: neighborsU.add(A)

        if A == v: neighborsV.add(B)
        else if B == v: neighborsV.add(A)

    // If either has no neighbors yet → allow
    if neighborsU.isEmpty() OR neighborsV.isEmpty():
        return true

    // Require at least one shared neighbor
    for each x in neighborsU:
        if neighborsV.contains(x):
            return true

    return false

function EdgeCrossesExisting(u, v, points, segments):

    p = points[u]
    q = points[v]

    for each (A,B) in segments:
        if (A == u AND B == v) OR (A == v AND B == u):
            continue // same edge

        r = points[A]
        s = points[B]

        if SegmentsProperlyIntersect(p,q, r,s):
            return true

    return false


(Where SegmentsProperlyIntersect is the strict segment–segment intersection test from your Geometry lib.)

RandomPermutation(X) just means: take the set/list X, shuffle with Fisher–Yates, iterate.

5. What is this algorithm actually doing?

At a high level:

You start with:

some initial segments (boundary + constraints),

a bunch of points.

On each iteration, you pick two points and try to grow a triangle that:

does not cross any existing segments,

uses only edges that satisfy the neighbor rule.

If you find such a triangle, you add its edges to Segments.
Those edges then become new constraints for all future steps.

You keep going until a whole outer loop finishes without adding any edges (changed = false) or you hit maxIterations.

Because:

Segments only ever grows (no edge removals),

the number of possible edges is finite (n*(n-1)/2),

and each legal triangle adds at least one new edge,

the process is monotone and must eventually stop.

When it stops, you have a graph in which:

no edge crosses any constraint or previously added edge,

no edge violates the neighbor rule,

and no more triangles that pass the enforcer can be added.

That’s your Triangle Garden: a maximal set of edges under your rules.
It’s not “classic textbook CDT”, but it is:

consistent with all constraints,

robust against your nightmare PSLG cases,

and conceptually simple enough that Codex is less likely to mutate into a paperclip factory.