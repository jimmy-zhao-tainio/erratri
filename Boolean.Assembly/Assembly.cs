using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Topology;

namespace Boolean;

public sealed class AssemblyInput
{
    public AssemblyInput(
        IntersectionGraph graph,
        TrianglePatches patches,
        BooleanPatchSet selected)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Patches = patches ?? throw new ArgumentNullException(nameof(patches));
        Selected = selected ?? throw new ArgumentNullException(nameof(selected));
    }

    public IntersectionGraph Graph { get; }
    public TrianglePatches Patches { get; }
    public BooleanPatchSet Selected { get; }
}

public sealed class AssemblyOutput
{
    public AssemblyOutput(RealMesh mesh)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
    }

    public RealMesh Mesh { get; }
}

public static class Assembly
{
    public static AssemblyOutput Run(AssemblyInput input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        var graph = input.Graph;
        var selected = input.Selected;

        Validate(graph, selected);

        var mesh = BooleanMeshAssembler.Assemble(selected);
        return new AssemblyOutput(mesh);
    }

    // Validates that selected boundary edges align with the intersection graph.
    private static void Validate(IntersectionGraph graph, BooleanPatchSet selected)
    {
        var graphEdges = new HashSet<(int Min, int Max)>(graph.Edges.Count);
        for (int i = 0; i < graph.Edges.Count; i++)
        {
            var e = graph.Edges[i];
            int a = e.Start.Value;
            int b = e.End.Value;
            if (b < a) (a, b) = (b, a);
            graphEdges.Add((a, b));
        }

        var graphPositionById = new Dictionary<int, RealPoint>(graph.Vertices.Count);
        for (int i = 0; i < graph.Vertices.Count; i++)
        {
            var v = graph.Vertices[i];
            graphPositionById[v.Id.Value] = v.Position;
        }

        var vertices = new List<RealPoint>();
        var triangles = new List<(int A, int B, int C)>();
        var vertexMap = new Dictionary<QuantizedVertexKey, int>();
        var vertexIntersectionIds = new List<int>();
        var provenance = new List<string>();

        int AddVertex(in RealPoint point)
        {
            int idx = VertexQuantizer.AddOrGet(vertices, vertexMap, point);
            while (vertexIntersectionIds.Count < vertices.Count)
            {
                vertexIntersectionIds.Add(-1);
            }
            return idx;
        }

        void AssignIntersectionId(int vertexIndex, int id)
        {
            if (id < 0)
            {
                return;
            }

            int existing = vertexIntersectionIds[vertexIndex];
            if (existing == -1)
            {
                vertexIntersectionIds[vertexIndex] = id;
                return;
            }

            if (existing != id)
            {
                vertexIntersectionIds[vertexIndex] = -1;
            }
        }

        void AddTri(in RealTriangle tri, TriangleVertexIds? vertexIds, string prov)
        {
            if (RealTriangle.HasZeroArea(tri.P0, tri.P1, tri.P2))
            {
                return;
            }

            int i0 = AddVertex(tri.P0);
            int i1 = AddVertex(tri.P1);
            int i2 = AddVertex(tri.P2);

            if (i0 == i1 || i1 == i2 || i2 == i0)
            {
                return;
            }

            triangles.Add((i0, i1, i2));
            provenance.Add(prov);

            if (vertexIds.HasValue)
            {
                var ids = vertexIds.Value;
                AssignIntersectionId(i0, ids.V0);
                AssignIntersectionId(i1, ids.V1);
                AssignIntersectionId(i2, ids.V2);
            }
        }

        var idsA = selected.IntersectionVertexIdsFromMeshA;
        if (idsA != null && idsA.Count != selected.FromMeshA.Count)
        {
            idsA = null;
        }

        var idsB = selected.IntersectionVertexIdsFromMeshB;
        if (idsB != null && idsB.Count != selected.FromMeshB.Count)
        {
            idsB = null;
        }

        for (int i = 0; i < selected.FromMeshA.Count; i++)
        {
            var ids = idsA != null ? idsA[i] : (TriangleVertexIds?)null;
            AddTri(selected.FromMeshA[i], ids, $"A#{i}");
        }

        for (int i = 0; i < selected.FromMeshB.Count; i++)
        {
            var ids = idsB != null ? idsB[i] : (TriangleVertexIds?)null;
            AddTri(selected.FromMeshB[i], ids, $"B#{i}");
        }

        _ = VertexWelder.WeldInPlace(vertices, triangles, provenance, Tolerances.MergeEpsilon, out var remap);

        var weldedIntersectionIds = new int[vertices.Count];
        for (int i = 0; i < weldedIntersectionIds.Length; i++)
        {
            weldedIntersectionIds[i] = -1;
        }

        for (int i = 0; i < vertexIntersectionIds.Count; i++)
        {
            int id = vertexIntersectionIds[i];
            if (id < 0)
            {
                continue;
            }

            int dst = remap[i];
            int existing = weldedIntersectionIds[dst];
            if (existing == -1)
            {
                weldedIntersectionIds[dst] = id;
            }
            else if (existing != id)
            {
                weldedIntersectionIds[dst] = -1;
            }
        }
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

            int ga = weldedIntersectionIds[key.Min];
            int gb = weldedIntersectionIds[key.Max];

            if (ga < 0 || gb < 0)
            {
                continue;
            }

            int ea = ga;
            int eb = gb;
            if (eb < ea) (ea, eb) = (eb, ea);

            if (graphEdges.Contains((ea, eb)))
            {
                continue;
            }

            if (IsGraphEdgeChain(in pa, in pb, ga, gb, graphEdges, graphPositionById))
            {
                continue;
            }

            int triIdx = firstTri.TryGetValue(key, out int t) ? t : -1;
            string triInfo = triIdx >= 0 && triIdx < triangles.Count
                ? $" tri#{triIdx}=({triangles[triIdx].A},{triangles[triIdx].B},{triangles[triIdx].C}) prov={provenance[triIdx]}"
                : string.Empty;

            throw new InvalidOperationException(
                "Pre-assembly invariant violated: selected boundary edge connects intersection vertices that are not adjacent in the intersection graph. " +
                $"edge=({key.Min},{key.Max}) A=({pa.X},{pa.Y},{pa.Z}) B=({pb.X},{pb.Y},{pb.Z}) graph=({ga},{gb}).{triInfo}");
        }
    }

    private static bool IsGraphEdgeChain(
        in RealPoint pa,
        in RealPoint pb,
        int ga,
        int gb,
        HashSet<(int Min, int Max)> graphEdges,
        Dictionary<int, RealPoint> graphPositionById)
    {
        if (ga == gb)
        {
            return true;
        }

        if (!graphPositionById.ContainsKey(ga) || !graphPositionById.ContainsKey(gb))
        {
            return false;
        }

        var ab = RealVector.FromPoints(in pa, in pb);
        double abLenSq = ab.Dot(in ab);
        if (abLenSq <= 0.0)
        {
            return false;
        }

        double eps = Tolerances.MergeEpsilon;
        double epsSq = eps * eps;

        var idToT = new Dictionary<int, double>();

        foreach (var kvp in graphPositionById)
        {
            int id = kvp.Key;
            var pos = kvp.Value;
            var ap = RealVector.FromPoints(in pa, in pos);
            double t = ap.Dot(in ab) / abLenSq;
            if (t < 0.0 || t > 1.0)
            {
                continue;
            }

            var closest = new RealPoint(
                pa.X + (pb.X - pa.X) * t,
                pa.Y + (pb.Y - pa.Y) * t,
                pa.Z + (pb.Z - pa.Z) * t);

            if (pos.DistanceSquared(in closest) > epsSq)
            {
                continue;
            }

            if (!idToT.ContainsKey(id))
            {
                idToT.Add(id, t);
            }
        }

        if (graphPositionById.TryGetValue(ga, out var posA) && !idToT.ContainsKey(ga))
        {
            var ap = RealVector.FromPoints(in pa, in posA);
            double t = ap.Dot(in ab) / abLenSq;
            idToT.Add(ga, t);
        }

        if (graphPositionById.TryGetValue(gb, out var posB) && !idToT.ContainsKey(gb))
        {
            var ap = RealVector.FromPoints(in pa, in posB);
            double t = ap.Dot(in ab) / abLenSq;
            idToT.Add(gb, t);
        }

        if (idToT.Count < 2)
        {
            return false;
        }

        var ordered = new List<(double T, int Id)>(idToT.Count);
        foreach (var kvp in idToT)
        {
            ordered.Add((kvp.Value, kvp.Key));
        }

        ordered.Sort(static (a, b) => a.T.CompareTo(b.T));

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            int a = ordered[i].Id;
            int b = ordered[i + 1].Id;
            if (a == b)
            {
                continue;
            }

            if (b < a) (a, b) = (b, a);
            if (!graphEdges.Contains((a, b)))
            {
                return false;
            }
        }

        return true;
    }

}
