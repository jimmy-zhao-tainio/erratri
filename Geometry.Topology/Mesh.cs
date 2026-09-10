using Geometry;

namespace Geometry.Topology;

// A lightweight collection wrapper for a set of triangles assumed to bound a closed volume.
public sealed class Mesh
{
    public IReadOnlyList<Triangle> Triangles => triangles;
    private readonly List<Triangle> triangles;

    public Mesh(IEnumerable<Triangle> triangles)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        this.triangles = new List<Triangle>();
        foreach (var triangle in triangles)
        {
            // Tetrahedron faces carry outward normals independently of their
            // vertex order. Meshes must encode that orientation in both forms.
            var a = new RealPoint(triangle.P0);
            var b = new RealPoint(triangle.P1);
            var c = new RealPoint(triangle.P2);
            var cross = RealVector.FromPoints(a, b).Cross(RealVector.FromPoints(a, c));
            double alignment = cross.X * triangle.Normal.X + cross.Y * triangle.Normal.Y + cross.Z * triangle.Normal.Z;
            this.triangles.Add(alignment < 0
                ? Triangle.FromWinding(triangle.P0, triangle.P2, triangle.P1)
                : triangle);
        }
    }

    public int Count => triangles.Count;

    // Factory: builds a Mesh from a collection of tetrahedra by
    // selecting only boundary triangles (those that appear exactly once).
    public static Mesh FromTetrahedra(IEnumerable<Tetrahedron> tetrahedra)
    {
        if (tetrahedra is null) throw new ArgumentNullException(nameof(tetrahedra));
        var triangleOccurrences = new Dictionary<TriangleKey, (int count, Triangle triangle)>();

        static void Accumulate(ref Dictionary<TriangleKey, (int count, Triangle triangle)> map, in Triangle triangle)
        {
            var key = TriangleKey.FromTriangle(triangle);
            if (map.TryGetValue(key, out var entry)) map[key] = (entry.count + 1, entry.triangle);
            else map[key] = (1, triangle);
        }

        foreach (var tetrahedron in tetrahedra)
        {
            Accumulate(ref triangleOccurrences, tetrahedron.ABC);
            Accumulate(ref triangleOccurrences, tetrahedron.ABD);
            Accumulate(ref triangleOccurrences, tetrahedron.ACD);
            Accumulate(ref triangleOccurrences, tetrahedron.BCD);
        }

        var boundary = new List<Triangle>();
        foreach (var pair in triangleOccurrences)
            if (pair.Value.count == 1) boundary.Add(pair.Value.triangle);

        return new Mesh(boundary);
    }
}