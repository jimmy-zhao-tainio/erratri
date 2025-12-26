using Geometry.Topology;
using Geometry;

namespace World;

public sealed class Cylinder : Shape
{
    private readonly List<Geometry.Tetrahedron> tetrahedra = new();
    public Cylinder(long radius, long thickness = 2, long height = 2,
                    Point? center = null,
                    int? segments = null,
                    double xTiltDeg = 0, double yTiltDeg = 0, double zSpinDeg = 0)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        // Ensure odd/even thickness creates two distinct radii.
        if (thickness < 2) thickness = 2; // enforce at least a one-unit shell on each side

        Center = center ?? new Point(0, 0, 0);
        InnerRadius = Math.Max(1, radius - thickness / 2);
        OuterRadius = radius + (thickness - thickness / 2);
        HalfHeight = height / 2; // integer division acceptable for grid rounding; ensures non-negative
        if (HalfHeight <= 0) HalfHeight = 1;

        Segments = segments ?? ChooseSegments(radius);
        if (Segments < 8) Segments = 8;

        BuildShell(xTiltDeg, yTiltDeg, zSpinDeg);
        Mesh = Mesh.FromTetrahedra(tetrahedra);
    }

    public long InnerRadius { get; }
    public long OuterRadius { get; }
    public long HalfHeight { get; }
    public int Segments { get; }
    public Point Center { get; }

    private static int ChooseSegments(long radius)
    {
        // Target ~30 units per chord, clamped to avoid exploding triangle count.
        double circumference = 2.0 * Math.PI * Math.Max(1, radius);
        int n = (int)Math.Round(circumference / 30.0);
        if (n < 64) n = 64;
        if (n > 720) n = 720;
        return n;
    }

    private static Point P(long x, long y, long z)
        => new Point(x, y, z);

    private void BuildShell(double xTiltDeg, double yTiltDeg, double zSpinDeg)
    {
        var cx = Center.X; var cy = Center.Y; var cz = Center.Z;
        long z0 = -HalfHeight;
        long z1 = +HalfHeight;

        // Precompute rotation sines/cosines for X->Y->Z order
        double rx = xTiltDeg * Math.PI / 180.0;
        double ry = yTiltDeg * Math.PI / 180.0;
        double rz = zSpinDeg * Math.PI / 180.0;
        double cxr = Math.Cos(rx), sxr = Math.Sin(rx);
        double cyr = Math.Cos(ry), syr = Math.Sin(ry);
        double czr = Math.Cos(rz), szr = Math.Sin(rz);

        (long X, long Y, long Z) R(long x, long y, long z)
        {
            // Convert to double, rotate in X->Y->Z order, then translate to center and round
            double xd = x, yd = y, zd = z;
            // X
            double y1 = yd * cxr - zd * sxr;
            double z1 = yd * sxr + zd * cxr;
            double x1 = xd;
            // Y
            double x2 = x1 * cyr + z1 * syr;
            double z2 = -x1 * syr + z1 * cyr;
            double y2 = y1;
            // Z
            double x3 = x2 * czr - y2 * szr;
            double y3 = x2 * szr + y2 * czr;
            double z3 = z2;
            return (
                (long)Math.Round(cx + x3, MidpointRounding.AwayFromZero),
                (long)Math.Round(cy + y3, MidpointRounding.AwayFromZero),
                (long)Math.Round(cz + z3, MidpointRounding.AwayFromZero)
            );
        }

        for (int i = 0; i < Segments; i++)
        {
            double a0 = (2.0 * Math.PI * i) / Segments;
            double a1 = (2.0 * Math.PI * ((i + 1) % Segments)) / Segments;

            // Round XY to grid; duplicates can occur for small delta angles â€”
            // we guard by skipping degenerate tetrahedra below.
            long i0x = (long)Math.Round(InnerRadius * Math.Cos(a0), MidpointRounding.AwayFromZero);
            long i0y = (long)Math.Round(InnerRadius * Math.Sin(a0), MidpointRounding.AwayFromZero);
            long i1x = (long)Math.Round(InnerRadius * Math.Cos(a1), MidpointRounding.AwayFromZero);
            long i1y = (long)Math.Round(InnerRadius * Math.Sin(a1), MidpointRounding.AwayFromZero);

            long o0x = (long)Math.Round(OuterRadius * Math.Cos(a0), MidpointRounding.AwayFromZero);
            long o0y = (long)Math.Round(OuterRadius * Math.Sin(a0), MidpointRounding.AwayFromZero);
            long o1x = (long)Math.Round(OuterRadius * Math.Cos(a1), MidpointRounding.AwayFromZero);
            long o1y = (long)Math.Round(OuterRadius * Math.Sin(a1), MidpointRounding.AwayFromZero);

            var bi0r = R(i0x, i0y, z0);
            var bo0r = R(o0x, o0y, z0);
            var bo1r = R(o1x, o1y, z0);
            var bi1r = R(i1x, i1y, z0);

            var ti0r = R(i0x, i0y, z1);
            var to0r = R(o0x, o0y, z1);
            var to1r = R(o1x, o1y, z1);
            var ti1r = R(i1x, i1y, z1);

            var bi0 = P(bi0r.X, bi0r.Y, bi0r.Z);
            var bo0 = P(bo0r.X, bo0r.Y, bo0r.Z);
            var bo1 = P(bo1r.X, bo1r.Y, bo1r.Z);
            var bi1 = P(bi1r.X, bi1r.Y, bi1r.Z);

            var ti0 = P(ti0r.X, ti0r.Y, ti0r.Z);
            var to0 = P(to0r.X, to0r.Y, to0r.Z);
            var to1 = P(to1r.X, to1r.Y, to1r.Z);
            var ti1 = P(ti1r.X, ti1r.Y, ti1r.Z);

            // Triangulate the wedge base into two triangles and extrude to prisms.
            // To ensure seam-aligned diagonals on rectangular faces between adjacent wedges,
            // alternate the prism decomposition across segments by swapping (b1,b2) <-> (t1,t2).
            // Deterministic diagonal rule (no parity):
            // - Outer wall diagonal: bo0 -> to1 (keep original prism A ordering)
            // - Inner wall diagonal: bi0 -> ti1 (achieved by flipping prism B ordering)
            // Prism A: (bi0, bo0, bo1) -> (ti0, to0, to1)
            AddPrismAsTets(bi0, bo0, bo1, ti0, to0, to1);
            // Prism B (flipped to enforce bi0 -> ti1 diagonal): (bi0, bi1, bo1) -> (ti0, ti1, to1)
            AddPrismAsTets(bi0, bi1, bo1, ti0, ti1, to1);
        }
    }

    // Standard triangular prism decomposition into 3 tetrahedra.
    private void AddPrismAsTets(in Point b0,
                                in Point b1,
                                in Point b2,
                                in Point t0,
                                in Point t1,
                                in Point t2)
    {
        TryAdd(b0, b1, b2, t2);
        TryAdd(b0, b1, t1, t2);
        TryAdd(b0, t0, t1, t2);
    }

    private void TryAdd(in Point a,
                        in Point b,
                        in Point c,
                        in Point d)
    {
        try
        {
            tetrahedra.Add(new Geometry.Tetrahedron(a, b, c, d));
        }
        catch (InvalidOperationException)
        {
            // Degenerate due to rounding/collinearity; skip.
        }
    }
}

