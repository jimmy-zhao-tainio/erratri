using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

// Selection is local to a subdivided face. A majority vote over connected
// patches can erase an intersection or propagate coplanar ownership across it.
public static class PatchSelector
{
    public static BooleanPatchSet Select(BooleanOperationType operation,
        PatchClassification classification, IntersectionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(classification);
        ArgumentNullException.ThrowIfNull(graph);
        if (!Enum.IsDefined(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
        var a = SelectMesh(classification.MeshA, operation, true);
        var b = SelectMesh(classification.MeshB, operation, false);
        return new BooleanPatchSet(a.Triangles, b.Triangles, a.Ids, b.Ids);
    }

    private static (List<RealTriangle> Triangles, List<TriangleVertexIds> Ids) SelectMesh(
        IReadOnlyList<IReadOnlyList<PatchInfo>> mesh, BooleanOperationType op, bool fromA)
    {
        var triangles = new List<RealTriangle>();
        var ids = new List<TriangleVertexIds>();
        foreach (var list in mesh)
            foreach (var patch in list)
            {
                bool keep;
                bool reverse = false;
                if (patch.CoplanarOwner != CoplanarOwner.None)
                {
                    // MeshA denotes aligned normals; MeshB denotes opposed normals.
                    bool aligned = patch.CoplanarOwner == CoplanarOwner.MeshA;
                    keep = op switch
                    {
                        BooleanOperationType.Union or BooleanOperationType.Intersection => aligned && fromA,
                        BooleanOperationType.DifferenceAB => !aligned && fromA,
                        BooleanOperationType.DifferenceBA => !aligned && !fromA,
                        BooleanOperationType.SymmetricDifference => false,
                        _ => false
                    };
                }
                else
                {
                    bool inside = patch.Containment == Containment.Inside;
                    bool outside = patch.Containment == Containment.Outside;
                    keep = op switch
                    {
                        BooleanOperationType.Union => outside,
                        BooleanOperationType.Intersection => inside,
                        BooleanOperationType.DifferenceAB => fromA ? outside : inside,
                        BooleanOperationType.DifferenceBA => fromA ? inside : outside,
                        BooleanOperationType.SymmetricDifference => inside || outside,
                        _ => false
                    };
                    reverse = inside && (op == BooleanOperationType.SymmetricDifference ||
                        (op == BooleanOperationType.DifferenceAB && !fromA) ||
                        (op == BooleanOperationType.DifferenceBA && fromA));
                }
                if (!keep) continue;
                var t = patch.Patch;
                var v = patch.VertexIds;
                triangles.Add(reverse ? new RealTriangle(t.P0, t.P2, t.P1) : t);
                ids.Add(reverse ? new TriangleVertexIds(v.V0, v.V2, v.V1) : v);
            }
        return (triangles, ids);
    }
}