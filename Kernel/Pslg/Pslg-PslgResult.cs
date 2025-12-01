using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

public sealed class PslgResult
{
    public Triangle Triangle { get; }
    public IReadOnlyList<TriangleSubdivision.IntersectionPoint> Points { get; }
    public IReadOnlyList<TriangleSubdivision.IntersectionSegment> Segments { get; }
    internal IReadOnlyList<PslgVertex> Vertices { get; }
    internal IReadOnlyList<PslgEdge> Edges { get; }
    internal IReadOnlyList<PslgHalfEdge> HalfEdges { get; }
    internal IReadOnlyList<PslgFace> Faces { get; }
    internal PslgFaceSelection Selection { get; }
    public IReadOnlyList<RealTriangle> Patches { get; }

    internal PslgResult(
        in PslgInput input,
        in PslgBuildState buildState,
        in PslgHalfEdgeState halfEdgeState,
        in PslgFaceState faceState,
        in PslgSelectionState selectionState,
        in PslgTriangulationState triangulationState)
    {
        Triangle = input.Triangle;
        Points = input.Points ?? throw new ArgumentNullException(nameof(input.Points));
        Segments = input.Segments ?? throw new ArgumentNullException(nameof(input.Segments));

        Vertices = buildState.Vertices ?? throw new ArgumentNullException(nameof(buildState.Vertices));
        Edges = buildState.Edges ?? throw new ArgumentNullException(nameof(buildState.Edges));
        HalfEdges = halfEdgeState.HalfEdges ?? throw new ArgumentNullException(nameof(halfEdgeState.HalfEdges));
        Faces = faceState.Faces ?? throw new ArgumentNullException(nameof(faceState.Faces));
        Selection = selectionState.Selection;
        Patches = triangulationState.Patches ?? throw new ArgumentNullException(nameof(triangulationState.Patches));
    }
}
