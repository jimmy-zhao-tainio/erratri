using System;
using System.Collections.Generic;

namespace Pslg;

public sealed class PslgOutput
{
    public IReadOnlyList<PslgPoint> Points { get; }
    public IReadOnlyList<PslgSegment> Segments { get; }
    public IReadOnlyList<PslgVertex> Vertices { get; }
    public IReadOnlyList<PslgEdge> Edges { get; }
    public IReadOnlyList<PslgHalfEdge> HalfEdges { get; }
    public IReadOnlyList<PslgFace> Faces { get; }
    public PslgFaceSelection Selection { get; }

    internal PslgOutput(
        in PslgBuildState buildState,
        in PslgHalfEdgeState halfEdgeState,
        in PslgFaceState faceState,
        in PslgSelectionState selectionState)
    {
        Points = buildState.Points ?? throw new ArgumentNullException(nameof(buildState.Points));
        Segments = buildState.Segments ?? throw new ArgumentNullException(nameof(buildState.Segments));
        Vertices = buildState.Vertices ?? throw new ArgumentNullException(nameof(buildState.Vertices));
        Edges = buildState.Edges ?? throw new ArgumentNullException(nameof(buildState.Edges));
        HalfEdges = halfEdgeState.HalfEdges ?? throw new ArgumentNullException(nameof(halfEdgeState.HalfEdges));
        Faces = faceState.Faces ?? throw new ArgumentNullException(nameof(faceState.Faces));
        Selection = selectionState.Selection;
    }
}
