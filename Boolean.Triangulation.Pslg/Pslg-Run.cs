using System;
using System.Collections.Generic;
using Geometry;
using Pslg.Phases;

namespace Pslg;

public static partial class PslgBuilder
{
    [ThreadStatic]
    private static PslgDebugSnapshot? _lastSnapshot;

    // High-level PSLG pipeline for a single triangle:
    //
    //   - builds the PSLG vertices/edges from intersection points/segments,
    //   - builds half-edges and faces,
    //   - selects interior faces with area checks,
    //   - captures a debug snapshot of all intermediate structures.
    //
    // Callers should prefer this entry point over invoking the individual
    // phases directly.
    public static PslgOutput Run(in PslgInput input)
    {
        input.Validate();

        var buildState = PslgBuildPhase.Run(in input);
        buildState.Validate();

        var halfEdgeState = PslgHalfEdgePhase.Run(buildState);
        halfEdgeState.Validate();

        var faceState = PslgFacePhase.Run(halfEdgeState);
        faceState.Validate();

        var selectionState = PslgSelectionPhase.Run(faceState);
        selectionState.Validate();

        SetDebugSnapshot(
            input.Triangle,
            buildState.Vertices,
            buildState.Edges,
            halfEdgeState.HalfEdges,
            faceState.Faces,
            selectionState.Selection);

        return new PslgOutput(
            buildState,
            halfEdgeState,
            faceState,
            selectionState);
    }

    private static void SetDebugSnapshot(
        in Triangle triangle,
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        IReadOnlyList<PslgHalfEdge> halfEdges,
        IReadOnlyList<PslgFace> faces,
        PslgFaceSelection selection)
    {
        _lastSnapshot = new PslgDebugSnapshot(triangle, vertices, edges, halfEdges, faces, selection);
    }

}
