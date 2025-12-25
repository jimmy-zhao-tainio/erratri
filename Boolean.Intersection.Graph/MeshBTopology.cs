using System;
using System.Collections.Generic;
using Geometry;
using Boolean.Intersection.Graph.Index;

namespace Boolean;

// Mesh-local intersection topology for mesh B.
//
// This describes which global intersection edges lie on which triangles
// of mesh B, how those edges are incident to global vertices, and the
// resulting closed intersection loops traced on mesh B.
public sealed class MeshBTopology : MeshTopology
{
    private MeshBTopology(
        IntersectionEdgeId[][] triangleEdges,
        IntersectionEdgeId[] edges,
        Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> vertexEdges,
        IntersectionVertexId[][] loops)
        : base(triangleEdges, edges, vertexEdges, loops)
    {
    }

    public static MeshBTopology Run(IntersectionGraph graph, TriangleIntersectionIndex index)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (index is null) throw new ArgumentNullException(nameof(index));

        var trianglesB = graph.IntersectionSet.TrianglesB
            ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesB));

        var perTriangleVertices = index.TrianglesB;
        if (trianglesB.Count != perTriangleVertices.Count)
        {
            throw new InvalidOperationException("Triangle count mismatch between IntersectionSet and TriangleIntersectionIndex for mesh B.");
        }

        var data = BuildCore(graph, perTriangleVertices, meshA: false);
        return new MeshBTopology(
            data.TriangleEdges,
            data.Edges,
            data.VertexEdges,
            data.Loops);
    }
}
