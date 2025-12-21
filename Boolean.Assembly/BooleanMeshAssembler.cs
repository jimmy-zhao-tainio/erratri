using System;
using System.Collections.Generic;
using Geometry;
using Topology;

namespace Boolean;

// Assembles a mesh from selected boolean patches by quantizing vertices and emitting indexed triangles.
// NOTE: Pipeline stages are split into separate files in Kernel/BooleanAssembly/.
public static class BooleanMeshAssembler
{
    internal static void DebugPrintCheckpointAllPatches(
        string tag,
        BooleanPatchSet patchSet)
    {
        if (!EdgeUseDiagnostics.DebugStage2Enabled)
        {
            return;
        }

        if (patchSet is null) throw new ArgumentNullException(nameof(patchSet));

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();
        var provenance = new List<string>();

        void AddTri(in RealTriangle tri, string src)
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
            provenance.Add(src);
        }

        for (int i = 0; i < patchSet.FromMeshA.Count; i++) AddTri(patchSet.FromMeshA[i], "A");
        for (int i = 0; i < patchSet.FromMeshB.Count; i++) AddTri(patchSet.FromMeshB[i], "B");

        _ = VertexWelder.WeldInPlace(vertices, triangles, provenance, Tolerances.MergeEpsilon);
        _ = TriangleCleanup.DeduplicateIgnoringWindingInPlace(triangles, provenance);

        var triangleSources = new List<char>(provenance.Count);
        for (int i = 0; i < provenance.Count; i++)
        {
            triangleSources.Add(provenance[i].StartsWith("B", StringComparison.Ordinal) ? 'B' : 'A');
        }

        EdgeUseDebug.Print(tag, vertices, triangles, top: 10, triangleSources: triangleSources);
    }

    public static RealMesh Assemble(BooleanPatchSet patchSet)
    {
        if (patchSet is null) throw new ArgumentNullException(nameof(patchSet));

        bool debug = EdgeUseDiagnostics.DebugStage2Enabled;

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();
        List<string>? provenance = debug ? new List<string>() : null;

        void AddPatch(in RealTriangle tri, string? prov)
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
            if (provenance is not null)
            {
                provenance.Add(prov ?? "");
            }
        }

        if (provenance is null)
        {
            foreach (var tri in patchSet.FromMeshA) AddPatch(tri, prov: null);
            foreach (var tri in patchSet.FromMeshB) AddPatch(tri, prov: null);
        }
        else
        {
            var provA = patchSet.ProvenanceFromMeshA;
            for (int i = 0; i < patchSet.FromMeshA.Count; i++)
            {
                AddPatch(patchSet.FromMeshA[i], provA is null ? $"A#{i}" : provA[i]);
            }

            var provB = patchSet.ProvenanceFromMeshB;
            for (int i = 0; i < patchSet.FromMeshB.Count; i++)
            {
                AddPatch(patchSet.FromMeshB[i], provB is null ? $"B#{i}" : provB[i]);
            }

            var triangleSources = new List<char>(provenance.Count);
            for (int i = 0; i < provenance.Count; i++)
            {
                triangleSources.Add(provenance[i].StartsWith("B", StringComparison.Ordinal) ? 'B' : 'A');
            }

            EdgeUseDebug.Print("asm_preWeld", vertices, triangles, top: 10, triangleSources: triangleSources);
        }

        var weldStats = provenance is null
            ? VertexWelder.WeldInPlace(vertices, triangles, Tolerances.MergeEpsilon)
            : VertexWelder.WeldInPlace(vertices, triangles, provenance, Tolerances.MergeEpsilon);

        if (provenance is not null)
        {
            var triangleSources = new List<char>(provenance.Count);
            for (int i = 0; i < provenance.Count; i++)
            {
                triangleSources.Add(provenance[i].StartsWith("B", StringComparison.Ordinal) ? 'B' : 'A');
            }

            EdgeUseDebug.Print("asm_postWeld", vertices, triangles, top: 10, triangleSources: triangleSources);
        }

        int dedupeRemoved = provenance is null
            ? DedupWithoutProvenance(triangles)
            : TriangleCleanup.DeduplicateIgnoringWindingInPlace(triangles, provenance);

        if (provenance is not null)
        {
            var triangleSources = new List<char>(provenance.Count);
            for (int i = 0; i < provenance.Count; i++)
            {
                triangleSources.Add(provenance[i].StartsWith("B", StringComparison.Ordinal) ? 'B' : 'A');
            }

            EdgeUseDebug.Print("asm_postDedupe", vertices, triangles, top: 10, triangleSources: triangleSources);
            Console.WriteLine($"[asm_stats] weldCollapsedRemoved={weldStats.CollapsedRemoved} dedupeRemoved={dedupeRemoved}");
        }

        ManifoldEdgeValidator.ValidateManifoldEdges(vertices, triangles);

        return new RealMesh(vertices, triangles);
    }

    private static int DedupWithoutProvenance(List<(int A, int B, int C)> triangles)
    {
        int before = triangles.Count;
        TriangleCleanup.DeduplicateIgnoringWindingInPlace(triangles);
        return before - triangles.Count;
    }
}
