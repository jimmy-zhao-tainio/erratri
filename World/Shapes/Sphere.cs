using Geometry.Topology;
using Geometry;

namespace World;

public sealed class Sphere : Shape
{
    public Sphere(long radius, Point center) : this(radius, ChooseSubdivisions(radius), center) { }

    public Sphere(long radius) : this(radius, ChooseSubdivisions(radius), new Point(0, 0, 0)) { }
    public Sphere(long radius, int subdivisions, Point? center = null)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (subdivisions < 0 || subdivisions > 4) throw new ArgumentOutOfRangeException(nameof(subdivisions), "Supported range: 0..4");

        Center = center ?? new Point(0, 0, 0);
        Radius = radius;
        Subdivisions = subdivisions;

        BuildIcosahedron(out var verts, out var faces);
        for (int i = 0; i < subdivisions; i++)
            Subdivide(ref verts, ref faces);

        // Project to sphere, round to grid, dedup
        var pointMap = new Dictionary<int, Point>(verts.Count);
        var unique = new Dictionary<Point, Point>();
        for (int i = 0; i < verts.Count; i++)
        {
            var p = ProjectToGrid(verts[i], Center, radius);
            // dedup by integer point
            if (!unique.TryGetValue(p, out var stored))
            {
                unique[p] = p;
                stored = p;
            }
            pointMap[i] = stored;
        }

        // Create star tetrahedra (center + each boundary triangle)
        var c = Center;
        var tetrahedra = new List<Geometry.Tetrahedron>(faces.Count);
        for (int i = 0; i < faces.Count; i++)
        {
            var (aIdx, bIdx, cIdx) = faces[i];
            var a = pointMap[aIdx];
            var b = pointMap[bIdx];
            var d = pointMap[cIdx];
            // Drop collapsed triangles (any duplicates)
            if (a.Equals(b) || a.Equals(d) || b.Equals(d)) continue;
            try
            {
                tetrahedra.Add(new Geometry.Tetrahedron(c, a, b, d));
            }
            catch (InvalidOperationException)
            {
                // Degenerate after rounding; skip
            }
        }

        Mesh = Mesh.FromTetrahedra(tetrahedra);

    }

    public long Radius { get; }
    public int Subdivisions { get; }
    public Point Center { get; }

    private static int ChooseSubdivisions(long radius)
    {
        return 3;
    }


    private readonly struct DVec
    {
        public readonly double X, Y, Z;
        public DVec(double x, double y, double z) { X = x; Y = y; Z = z; }
        public DVec Add(DVec b) => new DVec(X + b.X, Y + b.Y, Z + b.Z);
        public DVec Scale(double s) => new DVec(X * s, Y * s, Z * s);
        public DVec Normalize()
        {
            var len = Math.Sqrt(X * X + Y * Y + Z * Z);
            if (len == 0) return this;
            var inv = 1.0 / len;
            return new DVec(X * inv, Y * inv, Z * inv);
        }
    }

    private static Point ProjectToGrid(DVec v, Point center, long radius)
    {
        var n = v.Normalize().Scale(radius);
        long x = center.X + (long)Math.Round(n.X, MidpointRounding.AwayFromZero);
        long y = center.Y + (long)Math.Round(n.Y, MidpointRounding.AwayFromZero);
        long z = center.Z + (long)Math.Round(n.Z, MidpointRounding.AwayFromZero);
        return new Point(x, y, z);
    }

    private static void BuildIcosahedron(out List<DVec> verts, out List<(int a, int b, int c)> faces)
    {
        // Icosahedron vertices from golden ratio
        double phi = (1.0 + Math.Sqrt(5.0)) * 0.5; // Ï†
        verts = new List<DVec>
        {
            new DVec(-1,  phi, 0), new DVec(1,  phi, 0), new DVec(-1, -phi, 0), new DVec(1, -phi, 0),
            new DVec(0, -1,  phi), new DVec(0, 1,  phi), new DVec(0, -1, -phi), new DVec(0, 1, -phi),
            new DVec( phi, 0, -1), new DVec( phi, 0, 1), new DVec(-phi, 0, -1), new DVec(-phi, 0, 1)
        };
        // Normalize to unit sphere
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].Normalize();

        // 20 faces (indices into verts list)
        int[][] f = new int[][]
        {
            new[]{0,11,5}, new[]{0,5,1}, new[]{0,1,7}, new[]{0,7,10}, new[]{0,10,11},
            new[]{1,5,9}, new[]{5,11,4}, new[]{11,10,2}, new[]{10,7,6}, new[]{7,1,8},
            new[]{3,9,4}, new[]{3,4,2}, new[]{3,2,6}, new[]{3,6,8}, new[]{3,8,9},
            new[]{4,9,5}, new[]{2,4,11}, new[]{6,2,10}, new[]{8,6,7}, new[]{9,8,1}
        };
        faces = new List<(int a, int b, int c)>(f.Length);
        foreach (var t in f) faces.Add((t[0], t[1], t[2]));
    }

    private static void Subdivide(ref List<DVec> verts, ref List<(int a, int b, int c)> faces)
    {
        var v = verts; // work on a local to avoid ref capture in local function
        var cache = new Dictionary<(int, int), int>();
        int GetMid(int i, int j)
        {
            var key = i < j ? (i, j) : (j, i);
            if (cache.TryGetValue(key, out var idx)) return idx;
            var mid = v[i].Add(v[j]).Normalize();
            idx = v.Count;
            v.Add(mid);
            cache[key] = idx;
            return idx;
        }

        var newFaces = new List<(int a, int b, int c)>(faces.Count * 4);
        foreach (var (a, b, c) in faces)
        {
            int ab = GetMid(a, b);
            int bc = GetMid(b, c);
            int ca = GetMid(c, a);
            newFaces.Add((a, ab, ca));
            newFaces.Add((b, bc, ab));
            newFaces.Add((c, ca, bc));
            newFaces.Add((ab, bc, ca));
        }
        faces = newFaces;
        verts = v;
    }
}

