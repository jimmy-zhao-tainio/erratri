using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Mesh-local intersection topology for mesh A.
//
// This describes which global intersection edges lie on which triangles
// of mesh A, how those edges are incident to global vertices, and the
// resulting closed intersection loops traced on mesh A.
public sealed class MeshATopology : MeshTopology
{
    private MeshATopology(
        IntersectionEdgeId[][] triangleEdges,
        IntersectionEdgeId[] edges,
        Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> vertexEdges,
        IntersectionVertexId[][] loops)
        : base(triangleEdges, edges, vertexEdges, loops)
    {
    }

    public static MeshATopology Build(IntersectionGraph graph, TriangleIntersectionIndex index)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (index is null) throw new ArgumentNullException(nameof(index));

        var trianglesA = graph.IntersectionSet.TrianglesA
            ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesA));

        var perTriangleVertices = index.TrianglesA;
        if (trianglesA.Count != perTriangleVertices.Count)
        {
            throw new InvalidOperationException("Triangle count mismatch between IntersectionSet and TriangleIntersectionIndex for mesh A.");
        }

        var data = BuildCore(graph, perTriangleVertices, meshA: true);
        return new MeshATopology(
            data.TriangleEdges,
            data.Edges,
            data.VertexEdges,
            data.Loops);
    }
}
