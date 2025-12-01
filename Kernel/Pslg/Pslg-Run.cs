using System;
using System.Collections.Generic;
using Geometry;
using Kernel.Pslg.Phases;

namespace Kernel;

public static partial class PslgBuilder
{
    [ThreadStatic]
    private static PslgDebugSnapshot? _lastSnapshot;

    // High-level PSLG pipeline for a single triangle:
    //
    //   - builds the PSLG vertices/edges from intersection points/segments,
    //   - builds half-edges and faces,
    //   - selects interior faces with area checks,
    //   - triangulates interior faces back to RealTriangle patches,
    //   - captures a debug snapshot of all intermediate structures.
    //
    // Callers should prefer this entry point over invoking the individual
    // phases directly.
    public static PslgResult Run(in PslgInput input)
    {
        if (input.Points is null) throw new ArgumentNullException(nameof(input.Points));
        if (input.Segments is null) throw new ArgumentNullException(nameof(input.Segments));

        var buildState = PslgBuildPhase.Run(in input);
        buildState.Validate();

        var halfEdgeState = PslgHalfEdgePhase.Run(buildState);
        halfEdgeState.Validate();

        var faceState = PslgFacePhase.Run(halfEdgeState);
        faceState.Validate();

        var selectionState = PslgSelectionPhase.Run(faceState);
        selectionState.Validate();

        var triangulationState = PslgTriangulationPhase.Run(input.Triangle, selectionState);
        triangulationState.Validate();

        SetDebugSnapshot(
            input.Triangle,
            buildState.Vertices,
            buildState.Edges,
            halfEdgeState.HalfEdges,
            faceState.Faces,
            selectionState.Selection);

        return new PslgResult(
            input,
            buildState,
            halfEdgeState,
            faceState,
            selectionState,
            triangulationState);
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

    internal static List<(int A, int B, int C)> TriangulateSimple(
        int[] polygon,
        IReadOnlyList<PslgVertex> vertices,
        double expectedArea)
        => PslgTriangulationPhase.TriangulateSimple(polygon, vertices, expectedArea);
}
