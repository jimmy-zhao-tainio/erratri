using System;
using System.Collections.Generic;
using Geometry;
using Boolean;
using Boolean.Intersection.Graph.Index;
using Topology;

namespace Boolean.Pipeline;

public static class PipelineEntry
{
    public static RealMesh Run(Mesh left, Mesh right, BooleanOperationType op)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));

        var meshA = left;
        var meshB = right;

        bool debug = EdgeUseDiagnostics.DebugStage2Enabled;

        var set = new IntersectionSet(meshA.Triangles, meshB.Triangles);
        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var topoA = MeshATopology.Build(graph, index);
        var topoB = MeshBTopology.Build(graph, index);
        var patches = TrianglePatchSet.Build(graph, index, topoA, topoB);

        if (debug)
        {
            var allA = new List<RealTriangle>();
            var allB = new List<RealTriangle>();
            for (int i = 0; i < patches.TrianglesA.Count; i++)
            {
                var g = patches.TrianglesA[i];
                for (int p = 0; p < g.Count; p++) allA.Add(g[p]);
            }
            for (int i = 0; i < patches.TrianglesB.Count; i++)
            {
                var g = patches.TrianglesB[i];
                for (int p = 0; p < g.Count; p++) allB.Add(g[p]);
            }

            var allPatchSet = new BooleanPatchSet(allA, allB);
            BooleanMeshAssembler.DebugPrintCheckpointAllPatches("ckpt_allPatches", allPatchSet);

            var allPatches = new List<RealTriangle>();
            for (int i = 0; i < patches.TrianglesA.Count; i++)
            {
                var g = patches.TrianglesA[i];
                for (int p = 0; p < g.Count; p++) allPatches.Add(g[p]);
            }
            for (int i = 0; i < patches.TrianglesB.Count; i++)
            {
                var g = patches.TrianglesB[i];
                for (int p = 0; p < g.Count; p++) allPatches.Add(g[p]);
            }

            EdgeUseDiagnostics.PrintEdgeUseFromRealTriangles("ckpt_patches_all", allPatches);
        }

        var classification = PatchClassifier.Classify(set, patches);
        var selected = BooleanPatchClassifier.Select(op, classification);

        ValidateSelectedBoundaryEdgesMatchGraph(graph, selected);

        if (debug)
        {
            var allSelected = new List<RealTriangle>(selected.FromMeshA.Count + selected.FromMeshB.Count);
            for (int i = 0; i < selected.FromMeshA.Count; i++) allSelected.Add(selected.FromMeshA[i]);
            for (int i = 0; i < selected.FromMeshB.Count; i++) allSelected.Add(selected.FromMeshB[i]);
            EdgeUseDiagnostics.PrintEdgeUseFromRealTriangles("ckpt_selected_all", allSelected);
        }

        return BooleanMeshAssembler.Assemble(selected);
    }

    private static void ValidateSelectedBoundaryEdgesMatchGraph(IntersectionGraph graph, BooleanPatchSet selected)
    {
        var graphVertexByKey = new Dictionary<(long X, long Y, long Z), IntersectionVertexId>(graph.Vertices.Count);
        var ambiguousGraphKeys = new HashSet<(long X, long Y, long Z)>();
        double inv = 1.0 / Tolerances.TrianglePredicateEpsilon;

        for (int i = 0; i < graph.Vertices.Count; i++)
        {
            var v = graph.Vertices[i];
            var key = Quantize(v.Position, inv);
            if (ambiguousGraphKeys.Contains(key))
            {
                continue;
            }

            if (graphVertexByKey.TryGetValue(key, out var existing) && existing.Value != v.Id.Value)
            {
                graphVertexByKey.Remove(key);
                ambiguousGraphKeys.Add(key);
                continue;
            }

            graphVertexByKey[key] = v.Id;
        }

        var graphEdges = new HashSet<(int Min, int Max)>(graph.Edges.Count);
        for (int i = 0; i < graph.Edges.Count; i++)
        {
            var e = graph.Edges[i];
            int a = e.Start.Value;
            int b = e.End.Value;
            if (b < a) (a, b) = (b, a);
            graphEdges.Add((a, b));
        }

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();
        var provenance = new List<string>();

        void AddTri(in RealTriangle tri, string prov)
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
            provenance.Add(prov);
        }

        for (int i = 0; i < selected.FromMeshA.Count; i++) AddTri(selected.FromMeshA[i], $"A#{i}");
        for (int i = 0; i < selected.FromMeshB.Count; i++) AddTri(selected.FromMeshB[i], $"B#{i}");

        _ = VertexWelder.WeldInPlace(vertices, triangles, provenance, Tolerances.MergeEpsilon);
        _ = TriangleCleanup.DeduplicateIgnoringWindingInPlace(triangles, provenance);

        var edgeUse = new Dictionary<(int Min, int Max), int>();
        var firstTri = new Dictionary<(int Min, int Max), int>();

        void AddEdge(int triIndex, int u, int v)
        {
            if (u == v) return;
            var key = u < v ? (u, v) : (v, u);
            edgeUse[key] = edgeUse.TryGetValue(key, out int n) ? n + 1 : 1;
            if (!firstTri.ContainsKey(key))
            {
                firstTri[key] = triIndex;
            }
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            if (a == b || b == c || c == a)
            {
                continue;
            }

            AddEdge(i, a, b);
            AddEdge(i, b, c);
            AddEdge(i, c, a);
        }

        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 1)
            {
                continue;
            }

            var key = kvp.Key;
            var pa = vertices[key.Min];
            var pb = vertices[key.Max];

            if (!graphVertexByKey.TryGetValue(Quantize(pa, inv), out var ga) ||
                !graphVertexByKey.TryGetValue(Quantize(pb, inv), out var gb))
            {
                continue;
            }

            int ea = ga.Value;
            int eb = gb.Value;
            if (eb < ea) (ea, eb) = (eb, ea);

            if (graphEdges.Contains((ea, eb)))
            {
                continue;
            }

            int triIdx = firstTri.TryGetValue(key, out int t) ? t : -1;
            string triInfo = triIdx >= 0 && triIdx < triangles.Count
                ? $" tri#{triIdx}=({triangles[triIdx].A},{triangles[triIdx].B},{triangles[triIdx].C}) prov={provenance[triIdx]}"
                : string.Empty;

            throw new InvalidOperationException(
                $"Pre-assembly invariant violated: selected boundary edge connects intersection vertices that are not adjacent in the intersection graph. " +
                $"edge=({key.Min},{key.Max}) A=({pa.X},{pa.Y},{pa.Z}) B=({pb.X},{pb.Y},{pb.Z}) graph=({ga.Value},{gb.Value}).{triInfo}");
        }
    }

    private static (long X, long Y, long Z) Quantize(RealPoint p, double inv)
    {
        long qx = (long)Math.Round(p.X * inv);
        long qy = (long)Math.Round(p.Y * inv);
        long qz = (long)Math.Round(p.Z * inv);
        return (qx, qy, qz);
    }
}
