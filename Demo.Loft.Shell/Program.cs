using Geometry;
using World;

// A bending, twisting octagonal bell: millimetres are converted to a 0.01 mm grid.
// Every ring lies on an exact integer Z plane, including after XY rotation.
const int steps = 32;
var sections = new List<LoftSection>();
for (int i = 0; i <= steps; i++)
{
    double t = (double)i / steps;
    double waist = Math.Sin(Math.PI * t);
    double rx = 28 + 19 * t * t - 13 * waist;
    double ry = 22 + 13 * t * t - 9 * waist;
    double lip = i == 0 || i == steps ? 1.5 : 0;
    double angle = 105 * t * Math.PI / 180;
    double x = 28 * t * t;
    double y = 8 * Math.Sin(Math.PI * t);
    long z = (long)Math.Round(14000 * t);
    Point[] Ring(double a, double b)
    {
        const double chamfer = 5;
        var corners = new (double X, double Y)[]
        {
            (-a + chamfer, -b), (a - chamfer, -b), (a, -b + chamfer), (a, b - chamfer),
            (a - chamfer, b), (-a + chamfer, b), (-a, b - chamfer), (-a, -b + chamfer)
        };
        return corners.Select(p => new Point(
            (long)Math.Round(100 * (x + p.X * Math.Cos(angle) - p.Y * Math.Sin(angle))),
            (long)Math.Round(100 * (y + p.X * Math.Sin(angle) + p.Y * Math.Cos(angle))), z)).ToArray();
    }
    sections.Add(new LoftSection(Ring(rx + lip, ry + lip), Ring(rx - 3, ry - 3)));
}
var shell = new Loft(sections);
var world = new World.World();
world.Add(shell);
world.Save("twisted_shell.stl", coordinateScale: 0.01);
Console.WriteLine($"Twisted shell: {sections.Count} planar sections, {shell.Mesh.Count} triangles, 140 mm tall, 105 degree twist.");
Console.WriteLine($"Wrote {Path.GetFullPath("twisted_shell.stl")}");
