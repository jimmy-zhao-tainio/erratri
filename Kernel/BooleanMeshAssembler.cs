using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Assembles a watertight mesh from selected boolean patches by merging
// vertices with quantization and emitting indexed triangles.
public static class BooleanMeshAssembler
{
    public static BooleanMesh Assemble(BooleanPatchSet patchSet)
    {
        if (patchSet is null) throw new ArgumentNullException(nameof(patchSet));

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();

        void AddPatch(in RealTriangle tri)
        {
            int i0 = AddOrGet(vertices, tri.P0);
            int i1 = AddOrGet(vertices, tri.P1);
            int i2 = AddOrGet(vertices, tri.P2);
            triangles.Add((i0, i1, i2));
        }

        foreach (var tri in patchSet.FromMeshA)
        {
            AddPatch(tri);
        }

        foreach (var tri in patchSet.FromMeshB)
        {
            AddPatch(tri);
        }

        return new BooleanMesh(vertices, triangles);
    }

    private static int AddOrGet(List<RealPoint> vertices, in RealPoint point)
    {
        double eps = Tolerances.TrianglePredicateEpsilon;
        double inv = 1.0 / eps;
        long qx = (long)Math.Round(point.X * inv);
        long qy = (long)Math.Round(point.Y * inv);
        long qz = (long)Math.Round(point.Z * inv);

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            long vx = (long)Math.Round(v.X * inv);
            long vy = (long)Math.Round(v.Y * inv);
            long vz = (long)Math.Round(v.Z * inv);
            if (vx == qx && vy == qy && vz == qz)
            {
                return i;
            }
        }

        vertices.Add(point);
        return vertices.Count - 1;
    }
}
