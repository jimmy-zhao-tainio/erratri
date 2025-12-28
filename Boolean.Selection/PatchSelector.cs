using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

// Selects which patches to keep for a given boolean operation using
// patch-level inside/outside classification.
public static class PatchSelector
{
    public static BooleanPatchSet Select(
        BooleanOperationType operation,
        PatchClassification classification,
        IntersectionGraph graph)
    {
        if (classification is null) throw new ArgumentNullException(nameof(classification));
        if (graph is null) throw new ArgumentNullException(nameof(graph));

        bool hasInside = HasInside(classification.MeshA) || HasInside(classification.MeshB);

        var keptA = SelectMesh(classification.MeshA, graph, operation, fromMeshA: true, hasInside);
        var keptB = SelectMesh(classification.MeshB, graph, operation, fromMeshA: false, hasInside);

        return new BooleanPatchSet(keptA.Triangles, keptB.Triangles, keptA.VertexIds, keptB.VertexIds);
    }

    private static bool ShouldKeepFromA(BooleanOperationType op, Containment containment) => op switch
    {
        BooleanOperationType.Intersection => containment == Containment.Inside,
        BooleanOperationType.Union => containment == Containment.Outside,
        BooleanOperationType.DifferenceAB => containment == Containment.Outside,
        BooleanOperationType.DifferenceBA => containment == Containment.Inside,
        BooleanOperationType.SymmetricDifference => containment == Containment.Outside,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.")
    };

    private static bool ShouldKeepFromB(BooleanOperationType op, Containment containment) => op switch
    {
        BooleanOperationType.Intersection => containment == Containment.Inside,
        BooleanOperationType.Union => containment == Containment.Outside,
        BooleanOperationType.DifferenceAB => containment == Containment.Inside,
        BooleanOperationType.DifferenceBA => containment == Containment.Outside,
        BooleanOperationType.SymmetricDifference => containment == Containment.Outside,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.")
    };

    private static (IReadOnlyList<RealTriangle> Triangles, IReadOnlyList<TriangleVertexIds> VertexIds) SelectMesh(
        IReadOnlyList<IReadOnlyList<PatchInfo>> mesh,
        IntersectionGraph graph,
        BooleanOperationType operation,
        bool fromMeshA,
        bool hasInside)
    {
        var flat = Flatten(mesh);
        if (flat.Count == 0)
        {
            return (Array.Empty<RealTriangle>(), Array.Empty<TriangleVertexIds>());
        }

        var regionIds = BuildRegionIds(flat, graph);
        var regionContainment = BuildRegionContainment(flat, regionIds, out var regionOwner);

        var kept = new List<RealTriangle>(flat.Count);
        var keptIds = new List<TriangleVertexIds>(flat.Count);
        for (int i = 0; i < flat.Count; i++)
        {
            int region = regionIds[i];
            var owner = regionOwner.TryGetValue(region, out var regionCoplanar)
                ? regionCoplanar
                : CoplanarOwner.None;

            bool useCoplanar = hasInside &&
                               owner != CoplanarOwner.None &&
                               regionContainment.TryGetValue(region, out var regionState) &&
                               regionState == Containment.On;

            bool keep = operation == BooleanOperationType.Intersection && !hasInside && owner != CoplanarOwner.None
                ? false
                : useCoplanar
                ? ShouldKeepCoplanar(operation, owner, fromMeshA)
                : (fromMeshA
                    ? ShouldKeepFromA(operation, regionContainment[region])
                    : ShouldKeepFromB(operation, regionContainment[region]));

            if (keep)
            {
                kept.Add(flat[i].Patch);
                keptIds.Add(flat[i].VertexIds);
            }
        }

        return (kept, keptIds);
    }

    private static bool HasInside(
        IReadOnlyList<IReadOnlyList<PatchInfo>> mesh)
    {
        var flat = Flatten(mesh);
        for (int i = 0; i < flat.Count; i++)
        {
            var patch = flat[i];
            if (patch.Containment == Containment.Inside && patch.CoplanarOwner == CoplanarOwner.None)
            {
                return true;
            }
        }

        return false;
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

    private static Dictionary<int, Containment> BuildRegionContainment(
        IReadOnlyList<PatchInfo> patches,
        IReadOnlyList<int> regionIds,
        out Dictionary<int, CoplanarOwner> regionOwner)
    {
        var regionContainment = new Dictionary<int, Containment>();
        regionOwner = new Dictionary<int, CoplanarOwner>();
        var regionWeights = new Dictionary<int, RegionWeights>();
        double mixEps = Tolerances.TrianglePredicateEpsilon;

        for (int i = 0; i < patches.Count; i++)
        {
            int region = regionIds[i];
            var patch = patches[i];
            double area = Math.Abs(patch.Patch.SignedArea3D);

            if (!regionWeights.TryGetValue(region, out var weights))
            {
                weights = new RegionWeights();
            }

            switch (patch.Containment)
            {
                case Containment.Inside:
                    weights.Inside += area;
                    break;
                case Containment.Outside:
                    weights.Outside += area;
                    break;
                case Containment.On:
                    weights.On += area;
                    break;
            }

            regionWeights[region] = weights;

            if (patch.CoplanarOwner != CoplanarOwner.None && !regionOwner.ContainsKey(region))
            {
                regionOwner[region] = patch.CoplanarOwner;
            }
        }

        foreach (var kvp in regionWeights)
        {
            int region = kvp.Key;
            var weights = kvp.Value;

            if (SelectionDiagnostics.IsEnabled() && IsMixed(weights, mixEps))
            {
                SelectionDiagnostics.LogRegionMix(region, weights.Inside, weights.Outside, weights.On);
            }

            var containment = SelectContainment(weights);
            regionContainment[region] = containment;
        }

        return regionContainment;
    }

    private static bool IsMixed(RegionWeights weights, double epsilon)
    {
        int buckets = 0;
        if (weights.Inside > epsilon) buckets++;
        if (weights.Outside > epsilon) buckets++;
        if (weights.On > epsilon) buckets++;
        return buckets > 1;
    }

    private static Containment SelectContainment(RegionWeights weights)
    {
        if (weights.Inside >= weights.Outside && weights.Inside >= weights.On)
        {
            return Containment.Inside;
        }

        if (weights.Outside >= weights.Inside && weights.Outside >= weights.On)
        {
            return Containment.Outside;
        }

        return Containment.On;
    }

    private struct RegionWeights
    {
        public double Inside;
        public double Outside;
        public double On;
    }

    private static Dictionary<int, bool> BuildRegionAllInside(
        IReadOnlyList<PatchInfo> patches,
        IReadOnlyList<int> regionIds)
    {
        var regionAllInside = new Dictionary<int, bool>();
        for (int i = 0; i < patches.Count; i++)
        {
            int region = regionIds[i];
            if (!regionAllInside.ContainsKey(region))
            {
                regionAllInside[region] = true;
            }

            var containment = patches[i].Containment;
            if (containment != Containment.Inside && containment != Containment.On)
            {
                regionAllInside[region] = false;
            }
        }

        return regionAllInside;
    }

    private static bool ShouldKeepCoplanar(
        BooleanOperationType op,
        CoplanarOwner owner,
        bool fromMeshA)
    {
        switch (op)
        {
            case BooleanOperationType.Intersection:
                return fromMeshA && owner == CoplanarOwner.MeshA;
            case BooleanOperationType.Union:
            case BooleanOperationType.SymmetricDifference:
                return false;
            case BooleanOperationType.DifferenceAB:
                return fromMeshA && owner == CoplanarOwner.MeshA;
            case BooleanOperationType.DifferenceBA:
                return !fromMeshA && owner == CoplanarOwner.MeshB;
            default:
                throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation.");
        }
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

    private static EdgeKey NormalizeEdge(int a, int b)
    {
        return a <= b ? new EdgeKey(a, b) : new EdgeKey(b, a);
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly int A;
        public readonly int B;

        public EdgeKey(int a, int b)
        {
            A = a;
            B = b;
        }

        public bool Equals(EdgeKey other) => A.Equals(other.A) && B.Equals(other.B);
        public override bool Equals(object? obj) => obj is EdgeKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(A, B);
    }
}
