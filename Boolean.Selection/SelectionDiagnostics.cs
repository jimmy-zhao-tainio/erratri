using System;
using System.Collections.Generic;
using System.Globalization;
using Geometry;

namespace Boolean;

internal static class SelectionDiagnostics
{
    private const int DefaultEdgeLimit = 50;
    private const string DumpFlag = "ERRATRI_DUMP_BOUNDARY";

    internal static bool IsEnabled()
    {
        var flag = Environment.GetEnvironmentVariable(DumpFlag);
        return string.Equals(flag, "1", StringComparison.Ordinal);
    }

    internal static void LogRegionMix(int regionId, double insideWeight, double outsideWeight, double onWeight)
    {
        if (!IsEnabled())
        {
            return;
        }

        var message = string.Format(
            CultureInfo.InvariantCulture,
            "RegionMix region={0} inside={1} outside={2} on={3}",
            regionId,
            insideWeight,
            outsideWeight,
            onWeight);
        Console.WriteLine(message);
    }

    internal static void DumpIfEnabled(
        BooleanOperationType operation,
        PatchClassification classification,
        IntersectionGraph graph,
        BooleanPatchSet selected)
    {
        if (!IsEnabled())
        {
            return;
        }

        int edgeLimit = DefaultEdgeLimit;
        var limitValue = Environment.GetEnvironmentVariable("ERRATRI_DUMP_BOUNDARY_LIMIT");
        if (int.TryParse(limitValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            edgeLimit = parsed;
        }

        Dump(operation, classification, graph, selected, edgeLimit);
    }

    private static void Dump(
        BooleanOperationType operation,
        PatchClassification classification,
        IntersectionGraph graph,
        BooleanPatchSet selected,
        int edgeLimit)
    {
        var patchesA = BuildRecords("A", classification.MeshA, graph, startIndex: 0);
        var patchesB = BuildRecords("B", classification.MeshB, graph, startIndex: patchesA.Count);

        var allPatches = new List<PatchRecord>(patchesA.Count + patchesB.Count);
        allPatches.AddRange(patchesA);
        allPatches.AddRange(patchesB);

        var vertexMap = new Dictionary<QuantizedVertexKey, int>();
        var vertexPositions = new List<RealPoint>();

        int GetVertexId(in RealPoint point)
        {
            var key = QuantizedVertexKey.FromRealPoint(in point);
            if (vertexMap.TryGetValue(key, out int id))
            {
                return id;
            }

            int idx = vertexPositions.Count;
            vertexPositions.Add(point);
            vertexMap.Add(key, idx);
            return idx;
        }

        var preTriangleVertices = new (int A, int B, int C)[allPatches.Count];
        var preEdgeToTris = new Dictionary<EdgeKey, List<int>>();

        for (int i = 0; i < allPatches.Count; i++)
        {
            var tri = allPatches[i].Triangle;
            int v0 = GetVertexId(tri.P0);
            int v1 = GetVertexId(tri.P1);
            int v2 = GetVertexId(tri.P2);
            preTriangleVertices[i] = (v0, v1, v2);

            AddEdge(preEdgeToTris, NormalizeEdge(v0, v1), i);
            AddEdge(preEdgeToTris, NormalizeEdge(v1, v2), i);
            AddEdge(preEdgeToTris, NormalizeEdge(v2, v0), i);
        }

        var selectedRecords = new List<SelectedRecord>();
        var selectedEdgeToTris = new Dictionary<EdgeKey, List<int>>();
        var edgeUse = new Dictionary<EdgeKey, int>();
        var selectedPatchIds = new HashSet<(string Operand, int GlobalIndex)>();

        AddSelected(selected.FromMeshA, patchesA, "A");
        AddSelected(selected.FromMeshB, patchesB, "B");

        var histogram = new SortedDictionary<int, int>();
        foreach (var kvp in edgeUse)
        {
            int count = kvp.Value;
            histogram[count] = histogram.TryGetValue(count, out int n) ? n + 1 : 1;
        }

        Console.WriteLine($"SelectionDiagnostics op={operation} preA={patchesA.Count} preB={patchesB.Count} selA={selected.FromMeshA.Count} selB={selected.FromMeshB.Count}");
        Console.WriteLine("EdgeUse histogram: " + FormatHistogram(histogram));

        var interesting = new List<(EdgeKey Key, int Use)>();
        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 2)
            {
                interesting.Add((kvp.Key, kvp.Value));
            }
        }

        interesting.Sort(static (a, b) =>
        {
            int cmp = b.Use.CompareTo(a.Use);
            if (cmp != 0) return cmp;
            cmp = a.Key.A.CompareTo(b.Key.A);
            return cmp != 0 ? cmp : a.Key.B.CompareTo(b.Key.B);
        });

        int countEdges = Math.Min(edgeLimit, interesting.Count);
        Console.WriteLine($"Edges use!=2 (showing {countEdges} of {interesting.Count}, cap={edgeLimit})");

        for (int i = 0; i < countEdges; i++)
        {
            var entry = interesting[i];
            var key = entry.Key;
            var pa = vertexPositions[key.A];
            var pb = vertexPositions[key.B];
            Console.WriteLine($"edge=({key.A},{key.B}) use={entry.Use} A={FormatPoint(pa)} B={FormatPoint(pb)}");

            if (selectedEdgeToTris.TryGetValue(key, out var triList))
            {
                for (int t = 0; t < triList.Count; t++)
                {
                    var tri = selectedRecords[triList[t]];
                    Console.WriteLine(
                        $"  tri operand={tri.Operand} patchId={tri.PatchIndex} faceId={tri.FaceId} regionId={tri.RegionId} containment={tri.Containment} coplanarOwner={tri.CoplanarOwner} v0={FormatPoint(tri.P0)} v1={FormatPoint(tri.P1)} v2={FormatPoint(tri.P2)} triId={tri.SelectedIndex}");
                }
            }

            if (entry.Use == 1)
            {
                int preCount = preEdgeToTris.TryGetValue(key, out var preList) ? preList.Count : 0;
                int selCount = selectedEdgeToTris.TryGetValue(key, out var selList) ? selList.Count : 0;
                int dropped = Math.Max(0, preCount - selCount);
                Console.WriteLine($"  preTriangles={preCount} selected={selCount} dropped={dropped}");

                if (preList is not null && dropped > 0)
                {
                    int printed = 0;
                    for (int p = 0; p < preList.Count && printed < 3; p++)
                    {
                        int idx = preList[p];
                        var pre = allPatches[idx];
                        if (selectedPatchIds.Contains((pre.Operand, pre.GlobalIndex)))
                        {
                            continue;
                        }

                        Console.WriteLine(
                            $"  dropped operand={pre.Operand} patchId={pre.LocalIndex} faceId={pre.FaceId} regionId={pre.RegionId} containment={pre.Containment} coplanarOwner={pre.CoplanarOwner} v0={FormatPoint(pre.Triangle.P0)} v1={FormatPoint(pre.Triangle.P1)} v2={FormatPoint(pre.Triangle.P2)} triId={pre.GlobalIndex}");
                        printed++;
                    }
                }
            }
        }

        void AddSelected(IReadOnlyList<RealTriangle> triangles, List<PatchRecord> source, string operand)
        {
            var lookup = BuildPatchLookup(source, preTriangleVertices, operand);
            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                int v0 = GetVertexId(tri.P0);
                int v1 = GetVertexId(tri.P1);
                int v2 = GetVertexId(tri.P2);
                var key = TriKey.FromVertices(v0, v1, v2);

                PatchRecord? match = null;
                if (lookup.TryGetValue(key, out var list) && list.Count > 0)
                {
                    int globalIndex = list.Dequeue();
                    if (globalIndex >= 0 && globalIndex < allPatches.Count)
                    {
                        var candidate = allPatches[globalIndex];
                        if (candidate.Operand == operand)
                        {
                            match = candidate;
                        }
                    }
                }

                var record = new SelectedRecord(
                    operand,
                    match?.LocalIndex ?? -1,
                    match?.GlobalIndex ?? -1,
                    match?.FaceId ?? -1,
                    match?.RegionId ?? -1,
                    match?.Containment ?? Containment.Outside,
                    match?.CoplanarOwner ?? CoplanarOwner.None,
                    tri.P0,
                    tri.P1,
                    tri.P2,
                    selectedRecords.Count);

                int recordIndex = selectedRecords.Count;
                selectedRecords.Add(record);
                if (record.GlobalIndex >= 0)
                {
                    selectedPatchIds.Add((operand, record.GlobalIndex));
                }

                AddSelectedEdge(NormalizeEdge(v0, v1), recordIndex);
                AddSelectedEdge(NormalizeEdge(v1, v2), recordIndex);
                AddSelectedEdge(NormalizeEdge(v2, v0), recordIndex);
            }
        }

        void AddSelectedEdge(EdgeKey key, int triIndex)
        {
            if (!selectedEdgeToTris.TryGetValue(key, out var list))
            {
                list = new List<int>(capacity: 2);
                selectedEdgeToTris.Add(key, list);
            }
            list.Add(triIndex);

            edgeUse[key] = edgeUse.TryGetValue(key, out int n) ? n + 1 : 1;
        }
    }

    private static List<PatchRecord> BuildRecords(
        string operand,
        IReadOnlyList<IReadOnlyList<PatchInfo>> mesh,
        IntersectionGraph graph,
        int startIndex)
    {
        var flat = Flatten(mesh);
        var regionIds = BuildRegionIds(flat, graph);

        var records = new List<PatchRecord>(flat.Count);
        for (int i = 0; i < flat.Count; i++)
        {
            var patch = flat[i];
            records.Add(new PatchRecord(
                operand,
                i,
                startIndex + i,
                patch.FaceId,
                regionIds[i],
                patch.Containment,
                patch.CoplanarOwner,
                patch.Patch));
        }

        return records;
    }

    private static List<PatchInfo> Flatten(IReadOnlyList<IReadOnlyList<PatchInfo>> mesh)
    {
        var flat = new List<PatchInfo>();
        for (int i = 0; i < mesh.Count; i++)
        {
            var list = mesh[i];
            for (int j = 0; j < list.Count; j++)
            {
                flat.Add(list[j]);
            }
        }

        return flat;
    }

    private static int[] BuildRegionIds(
        IReadOnlyList<PatchInfo> patches,
        IntersectionGraph graph)
    {
        int n = patches.Count;
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;

        int Find(int x)
        {
            int root = x;
            while (parent[root] != root) root = parent[root];
            while (parent[x] != x)
            {
                int next = parent[x];
                parent[x] = root;
                x = next;
            }
            return root;
        }

        void Union(int a, int b)
        {
            int ra = Find(a);
            int rb = Find(b);
            if (ra == rb) return;
            parent[rb] = ra;
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

        var graphVertexByKey = new Dictionary<QuantizedVertexKey, int>(graph.Vertices.Count);
        var ambiguous = new HashSet<QuantizedVertexKey>();
        for (int i = 0; i < graph.Vertices.Count; i++)
        {
            var v = graph.Vertices[i];
            var key = QuantizedVertexKey.FromRealPoint(in v.Position);
            if (ambiguous.Contains(key))
            {
                continue;
            }

            if (graphVertexByKey.TryGetValue(key, out int existing) && existing != v.Id.Value)
            {
                graphVertexByKey.Remove(key);
                ambiguous.Add(key);
                continue;
            }

            graphVertexByKey[key] = v.Id.Value;
        }

        var weldedPositions = new List<RealPoint>();
        var voxelToIds = new Dictionary<(long X, long Y, long Z), List<int>>();

        var edgeToTris = new Dictionary<EdgeKey, List<int>>();

        for (int i = 0; i < n; i++)
        {
            var tri = patches[i].Patch;
            int v0 = VertexWeld.GetOrAddCanonicalId(tri.P0, Tolerances.MergeEpsilon, weldedPositions, voxelToIds);
            int v1 = VertexWeld.GetOrAddCanonicalId(tri.P1, Tolerances.MergeEpsilon, weldedPositions, voxelToIds);
            int v2 = VertexWeld.GetOrAddCanonicalId(tri.P2, Tolerances.MergeEpsilon, weldedPositions, voxelToIds);

            AddEdge(edgeToTris, NormalizeEdge(v0, v1), i);
            AddEdge(edgeToTris, NormalizeEdge(v1, v2), i);
            AddEdge(edgeToTris, NormalizeEdge(v2, v0), i);
        }

        var weldedToGraph = new int[weldedPositions.Count];
        var weldedHasGraph = new bool[weldedPositions.Count];
        for (int i = 0; i < weldedPositions.Count; i++)
        {
            var key = QuantizedVertexKey.FromRealPoint(weldedPositions[i]);
            if (graphVertexByKey.TryGetValue(key, out int gid))
            {
                weldedToGraph[i] = gid;
                weldedHasGraph[i] = true;
            }
        }

        foreach (var kvp in edgeToTris)
        {
            var list = kvp.Value;
            if (list.Count < 2)
            {
                continue;
            }

            if (IsCutEdge(kvp.Key, weldedHasGraph, weldedToGraph, graphEdges))
            {
                continue;
            }

            int first = list[0];
            for (int i = 1; i < list.Count; i++)
            {
                Union(first, list[i]);
            }
        }

        var regionIds = new int[n];
        for (int i = 0; i < n; i++)
        {
            regionIds[i] = Find(i);
        }

        return regionIds;
    }

    private static bool IsCutEdge(
        EdgeKey edge,
        bool[] weldedHasGraph,
        int[] weldedToGraph,
        HashSet<(int Min, int Max)> graphEdges)
    {
        if (!weldedHasGraph[edge.A] || !weldedHasGraph[edge.B])
        {
            return false;
        }

        int a = weldedToGraph[edge.A];
        int b = weldedToGraph[edge.B];
        if (a == b)
        {
            return false;
        }

        if (b < a) (a, b) = (b, a);
        return graphEdges.Contains((a, b));
    }

    private static Dictionary<TriKey, Queue<int>> BuildPatchLookup(
        List<PatchRecord> patches,
        IReadOnlyList<(int A, int B, int C)> preTriangleVertices,
        string operand)
    {
        var lookup = new Dictionary<TriKey, Queue<int>>();
        for (int i = 0; i < patches.Count; i++)
        {
            if (patches[i].Operand != operand)
            {
                continue;
            }

            var verts = preTriangleVertices[patches[i].GlobalIndex];
            var key = TriKey.FromVertices(verts.A, verts.B, verts.C);

            if (!lookup.TryGetValue(key, out var queue))
            {
                queue = new Queue<int>();
                lookup.Add(key, queue);
            }
            queue.Enqueue(patches[i].GlobalIndex);
        }

        return lookup;
    }

    private static void AddEdge(
        Dictionary<EdgeKey, List<int>> edgeToTris,
        EdgeKey key,
        int triIndex)
    {
        if (!edgeToTris.TryGetValue(key, out var list))
        {
            list = new List<int>(capacity: 2);
            edgeToTris.Add(key, list);
        }

        list.Add(triIndex);
    }

    private static EdgeKey NormalizeEdge(int a, int b)
        => a <= b ? new EdgeKey(a, b) : new EdgeKey(b, a);

    private static string FormatHistogram(SortedDictionary<int, int> histogram)
    {
        if (histogram.Count == 0)
        {
            return "empty";
        }

        var parts = new List<string>(histogram.Count);
        foreach (var kvp in histogram)
        {
            parts.Add($"{kvp.Key}->{kvp.Value}");
        }
        return string.Join(", ", parts);
    }

    private static string FormatPoint(in RealPoint p)
        => string.Format(CultureInfo.InvariantCulture, "({0},{1},{2})", p.X, p.Y, p.Z);

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly int A;
        public readonly int B;

        public EdgeKey(int a, int b)
        {
            A = a;
            B = b;
        }

        public bool Equals(EdgeKey other) => A == other.A && B == other.B;
        public override bool Equals(object? obj) => obj is EdgeKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(A, B);
    }

    private readonly struct TriKey : IEquatable<TriKey>
    {
        public readonly int A;
        public readonly int B;
        public readonly int C;

        private TriKey(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public static TriKey FromVertices(int a, int b, int c)
        {
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);
            return new TriKey(a, b, c);
        }

        public bool Equals(TriKey other) => A == other.A && B == other.B && C == other.C;
        public override bool Equals(object? obj) => obj is TriKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(A, B, C);
    }

    private readonly struct PatchRecord
    {
        public string Operand { get; }
        public int LocalIndex { get; }
        public int GlobalIndex { get; }
        public int FaceId { get; }
        public int RegionId { get; }
        public Containment Containment { get; }
        public CoplanarOwner CoplanarOwner { get; }
        public RealTriangle Triangle { get; }

        public PatchRecord(
            string operand,
            int localIndex,
            int globalIndex,
            int faceId,
            int regionId,
            Containment containment,
            CoplanarOwner coplanarOwner,
            RealTriangle triangle)
        {
            Operand = operand;
            LocalIndex = localIndex;
            GlobalIndex = globalIndex;
            FaceId = faceId;
            RegionId = regionId;
            Containment = containment;
            CoplanarOwner = coplanarOwner;
            Triangle = triangle;
        }
    }

    private readonly struct SelectedRecord
    {
        public string Operand { get; }
        public int PatchIndex { get; }
        public int GlobalIndex { get; }
        public int FaceId { get; }
        public int RegionId { get; }
        public Containment Containment { get; }
        public CoplanarOwner CoplanarOwner { get; }
        public RealPoint P0 { get; }
        public RealPoint P1 { get; }
        public RealPoint P2 { get; }
        public int SelectedIndex { get; }

        public SelectedRecord(
            string operand,
            int patchIndex,
            int globalIndex,
            int faceId,
            int regionId,
            Containment containment,
            CoplanarOwner coplanarOwner,
            RealPoint p0,
            RealPoint p1,
            RealPoint p2,
            int selectedIndex)
        {
            Operand = operand;
            PatchIndex = patchIndex;
            GlobalIndex = globalIndex;
            FaceId = faceId;
            RegionId = regionId;
            Containment = containment;
            CoplanarOwner = coplanarOwner;
            P0 = p0;
            P1 = p1;
            P2 = p2;
            SelectedIndex = selectedIndex;
        }
    }
}
