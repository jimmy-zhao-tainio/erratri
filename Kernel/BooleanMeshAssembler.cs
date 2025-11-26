using System;
using System.Collections.Generic;
using System.IO;
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
        var triangleSources = new List<string>();
        var vertexMap = new Dictionary<(long X, long Y, long Z), int>();

        void AddPatch(in RealTriangle tri)
        {
            int i0 = AddOrGet(vertices, vertexMap, tri.P0);
            int i1 = AddOrGet(vertices, vertexMap, tri.P1);
            int i2 = AddOrGet(vertices, vertexMap, tri.P2);
            if (IsDegenerate(tri.P0, tri.P1, tri.P2))
            {
                if (BooleanDebugSettings.LogSkippedDegenerate)
                {
                    Console.WriteLine($"BooleanMeshAssembler: skipped degenerate triangle ({tri.P0.X},{tri.P0.Y},{tri.P0.Z})|({tri.P1.X},{tri.P1.Y},{tri.P1.Z})|({tri.P2.X},{tri.P2.Y},{tri.P2.Z})");
                }
                return;
            }
            triangles.Add((i0, i1, i2));
            triangleSources.Add($"({tri.P0.X},{tri.P0.Y},{tri.P0.Z})|({tri.P1.X},{tri.P1.Y},{tri.P1.Z})|({tri.P2.X},{tri.P2.Y},{tri.P2.Z})");
        }

        foreach (var tri in patchSet.FromMeshA)
        {
            AddPatch(tri);
        }

        foreach (var tri in patchSet.FromMeshB)
        {
            AddPatch(tri);
        }

        if (BooleanDebugSettings.EnableABEdgeAuditor)
        {
            AuditEdgeUsage(vertices, triangles, patchSet.FromMeshA.Count, triangleSources);
        }

        ValidateManifoldEdges(vertices, triangles, triangleSources);

        return new BooleanMesh(vertices, triangles);
    }

    private static int AddOrGet(
        List<RealPoint> vertices,
        Dictionary<(long X, long Y, long Z), int> map,
        in RealPoint point)
    {
        double eps = Tolerances.TrianglePredicateEpsilon;
        double inv = 1.0 / eps;
        long qx = (long)Math.Round(point.X * inv);
        long qy = (long)Math.Round(point.Y * inv);
        long qz = (long)Math.Round(point.Z * inv);

        var key = (qx, qy, qz);
        if (map.TryGetValue(key, out var existing))
        {
            return existing;
        }

        vertices.Add(point);
        int idx = vertices.Count - 1;
        map[key] = idx;
        return idx;
    }

    private static void ValidateManifoldEdges(
        IReadOnlyList<RealPoint> vertices,
        IReadOnlyList<(int A, int B, int C)> triangles,
        IReadOnlyList<string> triangleSources)
    {
        var edgeUse = new Dictionary<(int, int), int>();
        var edgeToTriangles = new Dictionary<(int, int), List<int>>();
        var idToPos = new Dictionary<int, RealPoint>();
        var voxelToIds = new Dictionary<(int, int, int), List<int>>();

        void AddEdge(int a, int b, int triIndex)
        {
            var key = a < b ? (a, b) : (b, a);
            edgeUse[key] = edgeUse.TryGetValue(key, out var count) ? count + 1 : 1;
            if (!edgeToTriangles.TryGetValue(key, out var list))
            {
                list = new List<int>();
                edgeToTriangles[key] = list;
            }
            list.Add(triIndex);
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (aIdx, bIdx, cIdx) = triangles[i];
            var pa = vertices[aIdx];
            var pb = vertices[bIdx];
            var pc = vertices[cIdx];

            int ca = VertexCanonicalizer.GetOrAddCanonicalId(pa, idToPos, voxelToIds);
            int cb = VertexCanonicalizer.GetOrAddCanonicalId(pb, idToPos, voxelToIds);
            int cc = VertexCanonicalizer.GetOrAddCanonicalId(pc, idToPos, voxelToIds);

            AddEdge(ca, cb, i);
            AddEdge(cb, cc, i);
            AddEdge(cc, ca, i);
        }

        var failures = new List<((int, int) Edge, int Count)>();
        string detailPath = "boolean_mesh_nonmanifold_detail.txt";
        bool detailWritten = false;

        bool firstDump = true;
        foreach (var kvp in edgeUse)
        {
            if (kvp.Value != 2)
            {
                failures.Add((kvp.Key, kvp.Value));
                TryDumpManifoldFailure(kvp.Key, idToPos, edgeToTriangles, triangleSources, append: !firstDump);
                firstDump = false;
            }
        }

        if (failures.Count > 0)
        {
            try
            {
                using var sw = new StreamWriter(detailPath, append: false);
                foreach (var failure in failures)
                {
                    var edge = failure.Edge;
                    sw.WriteLine($"edge=({edge.Item1},{edge.Item2}) count={failure.Count}");
                    var v0 = idToPos[edge.Item1];
                    var v1 = idToPos[edge.Item2];
                    sw.WriteLine($"  v0=({v0.X},{v0.Y},{v0.Z})");
                    sw.WriteLine($"  v1=({v1.X},{v1.Y},{v1.Z})");
                    if (edgeToTriangles.TryGetValue(edge, out var tris))
                    {
                        foreach (var idx in tris)
                        {
                            sw.WriteLine($"  tri {idx} source: {triangleSources[idx]}");
                        }
                    }
                    else
                    {
                        sw.WriteLine("  no triangles recorded for this edge?");
                    }
                }
                detailWritten = true;
            }
            catch
            {
                // best effort logging
            }

            var summary = string.Join("; ", failures.ConvertAll(f => $"edge {f.Edge} used {f.Count} times"));
            var message = $"Non-manifold edges detected in boolean mesh assembly ({failures.Count}): {summary}. Expected each to be used exactly 2 times.";
            if (detailWritten)
            {
                message += $" See {detailPath} for details.";
            }

            throw new InvalidOperationException(message);
        }
    }

    private static void AuditEdgeUsage(
        IReadOnlyList<RealPoint> vertices,
        IReadOnlyList<(int A, int B, int C)> triangles,
        int triangleCountMeshA,
        IReadOnlyList<string> triangleSources)
    {
        var edgeUsage = new Dictionary<(int, int), (int ACount, int BCount, List<int> ATri, List<int> BTri)>();
        var idToPos = new Dictionary<int, RealPoint>();
        var voxelToIds = new Dictionary<(int, int, int), List<int>>();

        void AddEdge(int a, int b, bool isA, int triIndex)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!edgeUsage.TryGetValue(key, out var entry))
            {
                entry = (0, 0, new List<int>(), new List<int>());
            }

            if (isA)
            {
                entry.ACount++;
                entry.ATri.Add(triIndex);
            }
            else
            {
                entry.BCount++;
                entry.BTri.Add(triIndex);
            }

            edgeUsage[key] = entry;
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var (aIdx, bIdx, cIdx) = triangles[i];
            var pa = vertices[aIdx];
            var pb = vertices[bIdx];
            var pc = vertices[cIdx];

            int ca = VertexCanonicalizer.GetOrAddCanonicalId(pa, idToPos, voxelToIds);
            int cb = VertexCanonicalizer.GetOrAddCanonicalId(pb, idToPos, voxelToIds);
            int cc = VertexCanonicalizer.GetOrAddCanonicalId(pc, idToPos, voxelToIds);

            bool isA = i < triangleCountMeshA;
            AddEdge(ca, cb, isA, i);
            AddEdge(cb, cc, isA, i);
            AddEdge(cc, ca, isA, i);
        }

        var path = "boolean_mesh_one_sided_edges.txt";
        bool any = false;
        try
        {
            using var sw = new StreamWriter(path, append: false);
            sw.WriteLine("=== SevereNonManifold edges (total count=1 or per-side>2) and OtherWeird (total count !=2 with mixed sides) ===");
            foreach (var kvp in edgeUsage)
            {
                var counts = kvp.Value;
                int total = counts.ACount + counts.BCount;
                bool severe = total == 1 || counts.ACount > 2 || counts.BCount > 2;
                bool oneSidedManifold = total == 2 && ((counts.ACount == 2 && counts.BCount == 0) || (counts.BCount == 2 && counts.ACount == 0));
                bool otherWeird = total != 2 && !severe;

                if (!severe && !otherWeird && (!BooleanDebugSettings.ABEdgeAuditorLogOneSidedManifold || !oneSidedManifold))
                {
                    continue;
                }

                any = true;
                var edge = kvp.Key;
                string bucket = severe ? "SevereNonManifold" : oneSidedManifold ? "OneSidedButLocallyManifold" : "OtherWeird";
                sw.WriteLine($"--- {bucket} edge=({edge.Item1},{edge.Item2}) ACount={counts.ACount} BCount={counts.BCount} ---");
                var v0 = idToPos[edge.Item1];
                var v1 = idToPos[edge.Item2];
                bool endpointsAreInteger = IsInteger(v0.X) && IsInteger(v0.Y) && IsInteger(v0.Z) &&
                                           IsInteger(v1.X) && IsInteger(v1.Y) && IsInteger(v1.Z);
                sw.WriteLine($"  v0=({v0.X},{v0.Y},{v0.Z})");
                sw.WriteLine($"  v1=({v1.X},{v1.Y},{v1.Z})");
                sw.WriteLine($"  IntegerEndpoints={endpointsAreInteger}");

                if (counts.ATri.Count > 0)
                {
                    sw.WriteLine("  MeshA tris:");
                    foreach (var t in counts.ATri)
                    {
                        sw.WriteLine($"    tri {t}: {triangleSources[t]}");
                    }
                }

                if (counts.BTri.Count > 0)
                {
                    sw.WriteLine("  MeshB tris:");
                    foreach (var t in counts.BTri)
                    {
                        sw.WriteLine($"    tri {t}: {triangleSources[t]}");
                    }
                }
            }
        }
        catch
        {
            // best effort only
        }

        if (any)
        {
            Console.WriteLine($"Boolean edge auditor: one-sided or odd-count edges logged to {path}");
        }
    }

    private static bool IsInteger(double value)
    {
        return Math.Abs(value - Math.Round(value)) <= 0.0;
    }

    private static bool IsDegenerate(in RealPoint p0, in RealPoint p1, in RealPoint p2)
    {
        double v0x = p1.X - p0.X;
        double v0y = p1.Y - p0.Y;
        double v0z = p1.Z - p0.Z;

        double v1x = p2.X - p0.X;
        double v1y = p2.Y - p0.Y;
        double v1z = p2.Z - p0.Z;

        double cx = v0y * v1z - v0z * v1y;
        double cy = v0z * v1x - v0x * v1z;
        double cz = v0x * v1y - v0y * v1x;

        double lenSq = cx * cx + cy * cy + cz * cz;
        const double epsSq = 1e-20;
        return lenSq < epsSq;
    }

    private static void TryDumpManifoldFailure(
        (int, int) edge,
        IReadOnlyDictionary<int, RealPoint> idToPos,
        Dictionary<(int, int), List<int>> edgeToTriangles,
        IReadOnlyList<string> triangleSources,
        bool append)
    {
        var path = "boolean_mesh_nonmanifold_dump.txt";
        try
        {
            using var sw = new StreamWriter(path, append: append);
            sw.WriteLine($"edge=({edge.Item1},{edge.Item2})");
            if (idToPos.TryGetValue(edge.Item1, out var p0) && idToPos.TryGetValue(edge.Item2, out var p1))
            {
                sw.WriteLine($"  v0=({p0.X},{p0.Y},{p0.Z})");
                sw.WriteLine($"  v1=({p1.X},{p1.Y},{p1.Z})");
            }
            if (edgeToTriangles.TryGetValue(edge, out var tris))
            {
                sw.WriteLine("triangles using this edge:");
                foreach (var idx in tris)
                {
                    sw.WriteLine($"  tri {idx}: {triangleSources[idx]}");
                }
            }
            else
            {
                sw.WriteLine("no triangles recorded for this edge?");
            }
            Console.WriteLine($"Non-manifold edge dump written to {path}");
        }
        catch
        {
            // best effort only
        }
    }
}

public static class BooleanDebugSettings
{
    // Enable to log A/B edge usage; default off to avoid noise.
    public static bool EnableABEdgeAuditor = true;

    // If true, also log one-sided but locally manifold edges (ACount==2, BCount==0 or vice versa).
    public static bool ABEdgeAuditorLogOneSidedManifold = false;

    // Log when degenerate triangles are skipped during assembly.
    public static bool LogSkippedDegenerate = false;
}

internal static class VertexCanonicalizer
{
    public const double MergeEpsilon = 1e-12;
    private const double MergeEpsilonSq = MergeEpsilon * MergeEpsilon;

    public static int GetOrAddCanonicalId(
        RealPoint p,
        Dictionary<int, RealPoint> idToPos,
        Dictionary<(int, int, int), List<int>> voxelToIds)
    {
        double inv = 1.0 / MergeEpsilon;
        int vx = (int)Math.Floor(p.X * inv);
        int vy = (int)Math.Floor(p.Y * inv);
        int vz = (int)Math.Floor(p.Z * inv);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    var key = (vx + dx, vy + dy, vz + dz);
                    if (!voxelToIds.TryGetValue(key, out var candidates))
                    {
                        continue;
                    }

                    foreach (var id in candidates)
                    {
                        var c = idToPos[id];
                        double dxp = p.X - c.X;
                        double dyp = p.Y - c.Y;
                        double dzp = p.Z - c.Z;
                        double distSq = dxp * dxp + dyp * dyp + dzp * dzp;
                        if (distSq <= MergeEpsilonSq)
                        {
                            return id;
                        }
                    }
                }
            }
        }

        int newId = idToPos.Count;
        idToPos[newId] = p;
        var home = (vx, vy, vz);
        if (!voxelToIds.TryGetValue(home, out var list))
        {
            list = new List<int>();
            voxelToIds[home] = list;
        }
        list.Add(newId);
        return newId;
    }
}
