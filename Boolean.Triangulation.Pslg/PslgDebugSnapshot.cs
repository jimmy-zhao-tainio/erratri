using System.Collections.Generic;
using Geometry;

namespace Pslg;

internal sealed class PslgDebugSnapshot
{
    public Triangle Triangle { get; }
    public PslgVertex[] Vertices { get; }
    public PslgEdge[] Edges { get; }
    public PslgHalfEdge[] HalfEdges { get; }
    public PslgFace[] Faces { get; }
    public PslgFaceSelection Selection { get; }

    public PslgDebugSnapshot(
        in Triangle triangle,
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        IReadOnlyList<PslgHalfEdge> halfEdges,
        IReadOnlyList<PslgFace> faces,
        PslgFaceSelection selection)
    {
        Triangle = triangle;
        Vertices = vertices.ToArray();
        Edges = edges.ToArray();
        HalfEdges = halfEdges.ToArray();
        Faces = faces.ToArray();
        Selection = selection;
    }
}
