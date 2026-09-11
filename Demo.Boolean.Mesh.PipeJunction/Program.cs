using Boolean;
using Geometry.Topology;
using World;

// Union the material of two open-ended pipe shells crossing at 60 degrees.
// This tests shell union; it does not additionally drill away internal walls.
var main = new Cylinder(radius: 100, thickness: 40, height: 600, segments: 32);
var branch = new Cylinder(radius: 80, thickness: 32, height: 500,
    segments: 32, yTiltDeg: 60);
Validate(main.Mesh, "Main pipe");
Validate(branch.Mesh, "Branch pipe");
Console.WriteLine("Pipe junction: union at 60 degrees");
var junction = new Union(main, branch);
Validate(junction.Mesh, "Junction after integer-grid conversion");
var world = new World.World();
world.Add(junction);
world.Save("pipe_junction.stl");
Console.WriteLine($"Wrote {Path.GetFullPath("pipe_junction.stl")}");

static void Validate(Mesh mesh, string label)
{
    var real = BooleanMeshConverter.FromMesh(mesh);
    var edges = new Dictionary<(int, int), (int Count, int Direction)>();
    double volume = 0;
    foreach (var t in real.Triangles)
    {
        var a = real.Vertices[t.A];
        var b = real.Vertices[t.B];
        var c = real.Vertices[t.C];
        volume += (a.X * (b.Y * c.Z - b.Z * c.Y) +
                   a.Y * (b.Z * c.X - b.X * c.Z) +
                   a.Z * (b.X * c.Y - b.Y * c.X)) / 6;
        foreach (var (u, v) in new[] { (t.A, t.B), (t.B, t.C), (t.C, t.A) })
        {
            var key = u < v ? (u, v) : (v, u);
            edges.TryGetValue(key, out var prior);
            edges[key] = (prior.Count + 1, prior.Direction + (u < v ? 1 : -1));
        }
    }
    int bad = edges.Count(e => e.Value != (2, 0));
    if (bad != 0 || volume <= 0 || real.Triangles.Count == 0)
        throw new InvalidOperationException($"{label}: {bad} invalid edges, signed volume {volume}");
    Console.WriteLine($"{label}: {mesh.Count} triangles, closed oriented edges, volume {volume:F2}");
}
