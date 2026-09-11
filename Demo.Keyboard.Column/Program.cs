using Boolean;
using World;

// One grid unit = 0.01 mm. Dimensions below stay integer throughout modeling.
const long mountSize = 2000;
const long switchCutout = 1400;
const long plateThickness = 150;
const long keyPitch = 1905;
const double tiltDegrees = 12;
// Centers follow a circular arc; adjacent plate tangents meet halfway between keys.
long rise = (long)Math.Round(keyPitch * Math.Tan(tiltDegrees * Math.PI / 360));

Shape Place(Shape shape, int row) => shape
    .Rotate(xDegrees: row * tiltDegrees)
    .Translate(0, row * keyPitch, Math.Abs(row) * rise);

Shape Mount(int row)
{
    var plate = new Box(mountSize, mountSize, plateThickness)
        .Translate(-mountSize / 2, -mountSize / 2, -plateThickness);
    var hole = new Box(switchCutout, switchCutout, 1000)
        .Translate(-switchCutout / 2, -switchCutout / 2, -500);
    return Place(new DifferenceAB(plate, hole), row);
}

Console.WriteLine("MX column: three 20 mm mounts, 19.05 mm pitch, 12 degree steps");
Shape column = Mount(-1);
// Join disjoint outer mounts before the centre: sequential -1,0,1 currently
// triggers a MeshConformer degeneracy in the boolean kernel.
foreach (int row in new[] { 1, 0 })
{
    Console.WriteLine($"Union finished MX mount {row}");
    column = new Union(column, Mount(row));
}
var mesh = BooleanMeshConverter.FromMesh(column.Mesh);
var edges = new Dictionary<(int, int), (int Count, int Direction)>();
var neighbors = new Dictionary<int, HashSet<int>>();
double volume = 0;
foreach (var t in mesh.Triangles)
{
    var a = mesh.Vertices[t.A]; var b = mesh.Vertices[t.B]; var c = mesh.Vertices[t.C];
    volume += (a.X * (b.Y * c.Z - b.Z * c.Y) +
               a.Y * (b.Z * c.X - b.X * c.Z) +
               a.Z * (b.X * c.Y - b.Y * c.X)) / 6;
    foreach (var (u, v) in new[] { (t.A, t.B), (t.B, t.C), (t.C, t.A) })
    {
        var key = u < v ? (u, v) : (v, u);
        edges.TryGetValue(key, out var old);
        edges[key] = (old.Count + 1, old.Direction + (u < v ? 1 : -1));
        if (!neighbors.ContainsKey(u)) neighbors[u] = new();
        if (!neighbors.ContainsKey(v)) neighbors[v] = new();
        neighbors[u].Add(v); neighbors[v].Add(u);
    }
}
if (edges.Count == 0 || edges.Any(e => e.Value != (2, 0)) || volume <= 0)
    throw new InvalidOperationException("Column is not a closed, consistently oriented mesh with positive volume.");
var seen = new HashSet<int>();
var pending = new Stack<int>();
pending.Push(neighbors.Keys.First());
while (pending.TryPop(out int v))
    if (seen.Add(v)) foreach (int next in neighbors[v]) pending.Push(next);
if (seen.Count != neighbors.Count)
    throw new InvalidOperationException("Key mounts are disconnected.");
// A connected closed surface with three through-holes has Euler characteristic -4.
if (neighbors.Count - edges.Count + mesh.Triangles.Count != -4)
    throw new InvalidOperationException("Expected three through-holes in the connected column.");

var world = new World.World();
world.Add(column);
world.Save("mx_curved_column.stl", coordinateScale: 0.01);
Console.WriteLine($"Validated connected column with three through-holes: {mesh.Triangles.Count} triangles, {volume / 1_000_000:F2} mm^3");
Console.WriteLine($"Wrote {Path.GetFullPath("mx_curved_column.stl")} (millimetre coordinates)");
