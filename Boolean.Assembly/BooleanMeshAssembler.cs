using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Topology;

namespace Boolean;

// Assembles a mesh from selected boolean patches by quantizing vertices and emitting indexed triangles.
public static class BooleanMeshAssembler
{
    public static RealMesh Assemble(BooleanPatchSet patchSet)
    {
        if (patchSet is null) throw new ArgumentNullException(nameof(patchSet));

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();

        void AddPatch(in RealTriangle tri)
        {
            if (RealTriangle.HasZeroArea(tri.P0, tri.P1, tri.P2))
            {
                return;
            }

            int i0 = VertexQuantizer.AddOrGet(vertices, vertexMap, tri.P0);
            int i1 = VertexQuantizer.AddOrGet(vertices, vertexMap, tri.P1);
            int i2 = VertexQuantizer.AddOrGet(vertices, vertexMap, tri.P2);

            if (i0 == i1 || i1 == i2 || i2 == i0)
            {
                return;
            }

            triangles.Add((i0, i1, i2));
        }

        foreach (var tri in patchSet.FromMeshA) AddPatch(tri);
        foreach (var tri in patchSet.FromMeshB) AddPatch(tri);

        _ = VertexWelder.WeldInPlace(vertices, triangles, Tolerances.MergeEpsilon);
        TriangleCleanup.DeduplicateIgnoringWindingInPlace(triangles);

        ManifoldEdgeValidator.ValidateManifoldEdges(vertices, triangles);

        return new RealMesh(vertices, triangles);
    }
}
