using Xunit;

namespace Geometry.Tests;

public class TetrahedronTests
{
    [Fact]
    public void Constructor_PositiveOrientation_VolumeMatches()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d = new Point(0, 0, 1);

        var tet = new Tetrahedron(a, b, c, d);

        AssertInRange(tet.Volume, 1.0 / 6.0, 1e-12);

        AssertOutwardNormals(tet);
        AssertFaceVertices(tet, a, b, c, d);
        AssertUnitNormals(tet);
    }

    [Fact]
    public void RangeEnforcement_BoundsAccepted_BeyondBoundsRejected()
    {
        long M = (1L << 62) - 1;
        // At bounds: should succeed
        var a = new Point(-M, -M, -M);
        var b = new Point(M, -M, -M);
        var c = new Point(-M, M, -M);
        var d = new Point(-M, -M, M);
        var ok = new Tetrahedron(a, b, c, d);
        Assert.True(ok.Volume > 0);

        // Beyond bounds: should throw
        long B = (1L << 62); // one past allowed max
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tetrahedron(new Point(B, 0, 0), b, c, d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tetrahedron(a, new Point(0, -B, 0), c, d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tetrahedron(a, b, new Point(0, 0, B), d));
    }

    [Fact]
    public void TranslationInvariance_VolumeUnchanged_NormalsUnchanged()
    {
        var a = new Point(1, 2, 3);
        var b = new Point(5, 2, 4);
        var c = new Point(2, 7, 3);
        var d = new Point(3, 4, 9);
        var t0 = new Tetrahedron(a, b, c, d);

        var t = (dx: 1000L, dy: -2000L, dz: 500L);
        var a2 = a + t;
        var b2 = b + t;
        var c2 = c + t;
        var d2 = d + t;
        var t1 = new Tetrahedron(a2, b2, c2, d2);

        AssertInRange(t1.Volume, t0.Volume, 1e-12);
        // Normals remain valid and outward after scaling
        AssertAllFacesOrthogonal(t1);
        AssertOutwardNormals(t1, 1e-12);
    }

    [Fact]
    public void ScalingInvariance_NormalsUnchanged_VolumeScalesByCube()
    {
        var a = new Point(1, 2, 3);
        var b = new Point(5, 2, 4);
        var c = new Point(2, 7, 3);
        var d = new Point(3, 4, 9);
        var t0 = new Tetrahedron(a, b, c, d);

        long s = 5; // positive scale
        var t1 = new Tetrahedron(Scale(a, s), Scale(b, s), Scale(c, s), Scale(d, s));
        var r1 = t1.Volume / t0.Volume;
        AssertInRange(r1, s * s * s, 1e-12);
        AssertNormalsEqual(t0, t1, 1e-12);

        long sn = -3; // negative scale
        var t2 = new Tetrahedron(Scale(a, sn), Scale(b, sn), Scale(c, sn), Scale(d, sn));
        var r2 = t2.Volume / t0.Volume;
        AssertInRange(r2, (long)Math.Abs(sn * sn * sn), 1e-12);
        AssertAllFacesOrthogonal(t2);
        AssertOutwardNormals(t2, 1e-12);
    }

    [Fact]
    public void NearBound_SkinnyHeight_StillRobust()
    {
        long M = (1L << 62) - 10; // a little margin
        var a = new Point(M, 0, 0);
        var b = new Point(M - 1, 1, 0);
        var c = new Point(M - 1, 0, 1);
        var d = new Point(M - 1, 0, 2); // skinny height

        var tet = new Tetrahedron(a, b, c, d);
        Assert.True(tet.Volume > 0);
        AssertUnitNormals(tet, 1e-12);
        AssertAllFacesOrthogonal(tet);
        AssertFaceConsistency(tet);
    }

    [Fact]
    public void Randomized_Tetrahedra_PropertiesHold()
    {
        var rnd = new Random(12345);
        int attempts = 500;
        int successes = 0;
        for (int i = 0; i < attempts; i++)
        {
            var a = RandPoint(rnd, -1000, 1000);
            var b = RandPoint(rnd, -1000, 1000);
            var c = RandPoint(rnd, -1000, 1000);
            var d = RandPoint(rnd, -1000, 1000);
            try
            {
                var tet = new Tetrahedron(a, b, c, d);
                successes++;
                Assert.True(tet.Volume > 0);
                AssertUnitNormals(tet, 1e-10);
                AssertOutwardNormals(tet);
                AssertAllFacesOrthogonal(tet, 1e-10);
                AssertFaceConsistency(tet, 1e-10);
            }
            catch (InvalidOperationException)
            {
                // Degenerate; skip.
            }
            catch (ArgumentOutOfRangeException)
            {
                // Range exceeded; skip (should not happen given bounds, but included for safety).
            }
        }

        Assert.True(successes >= 50, $"Too few valid tetrahedra generated: {successes}");
    }
    [Fact]
    public void Constructor_NegativeOrientation_IsFlippedToPositive()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(0, 1, 0);
        var c = new Point(1, 0, 0); // swapped b and c compared to positive case
        var d = new Point(0, 0, 1);

        var tet = new Tetrahedron(a, b, c, d);

        AssertInRange(tet.Volume, 1.0 / 6.0, 1e-12);
        AssertOutwardNormals(tet);
        AssertUnitNormals(tet);
    }

    [Fact]
    public void Constructor_Degenerate_DuplicatePoints_Throws()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d = a; // duplicate

        Assert.Throws<InvalidOperationException>(() => new Tetrahedron(a, b, c, d));
    }

    [Fact]
    public void Constructor_Degenerate_Coplanar_Throws()
    {
        // All points coplanar on z=0
        var a = new Point(0, 0, 0);
        var b = new Point(2, 0, 0);
        var c = new Point(0, 2, 0);
        var d = new Point(1, 1, 0);

        Assert.Throws<InvalidOperationException>(() => new Tetrahedron(a, b, c, d));
    }

    [Fact]
    public void Faces_AreOutward_ForLargeCoordinates()
    {
        long L = 1_000_000_000; // 1e9
        var a = new Point(0, 0, 0);
        var b = new Point(L, 0, 0);
        var c = new Point(0, L, 0);
        var d = new Point(0, 0, L);

        var tet = new Tetrahedron(a, b, c, d);
        Assert.True(tet.Volume > 0);

        AssertOutwardNormals(tet);
        AssertUnitNormals(tet, 1e-10);

        var expectedVol = (double)L * L * L / 6.0; // real volume
        // Use fixed absolute tolerance suited to double rounding at this magnitude.
        Assert.InRange(Math.Abs(tet.Volume - expectedVol), 0, 1e12);
    }

    [Fact]
    public void Permutations_AllYieldPositiveVolume()
    {
        var pts = new[]
        {
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 3, 0),
            new Point(0, 0, 5)
        };

        var perms = Permute(pts);
        foreach (var p in perms)
        {
            var tet = new Tetrahedron(p[0], p[1], p[2], p[3]);
            // For these axis-aligned points, expected real volume is 5.0
            AssertInRange(tet.Volume, 5.0, 1e-12);
            AssertOutwardNormals(tet);
            AssertUnitNormals(tet);
            AssertAllFacesOrthogonal(tet);
        }
    }

    [Fact]
    public void SkewedTranslatedTetrahedron_InvariantsHold()
    {
        var a = new Point(10, -2, 3);
        var b = new Point(13, 5, -1);
        var c = new Point(-4, 2, 7);
        var d = new Point(1, -3, 9);

        var tet = new Tetrahedron(a, b, c, d);

        Assert.True(tet.Volume > 0, "Volume must be positive after canonicalization.");
        AssertUnitNormals(tet);
        AssertOutwardNormals(tet);
        AssertAllFacesOrthogonal(tet);
    }

    private static void AssertInRange(double actual, double expected, double tol)
    {
        Assert.InRange(Math.Abs(actual - expected), 0, tol);
    }

    private static void AssertUnitNormals(in Tetrahedron tet, double tol = 1e-12)
    {
        AssertInRange(tet.ABC.Normal.Length(), 1.0, tol);
        AssertInRange(tet.ABD.Normal.Length(), 1.0, tol);
        AssertInRange(tet.ACD.Normal.Length(), 1.0, tol);
        AssertInRange(tet.BCD.Normal.Length(), 1.0, tol);
    }

    private static void AssertFaceVertices(Tetrahedron tet, Point a, Point b, Point c, Point d)
    {
        Assert.Equal(a, tet.A);
        Assert.Equal(b, tet.B);
        // C and D may be swapped by the canonicalization step, so check as a set for ABC/ABD/ACD/BCD
        var faces = new HashSet<(Point, Point, Point)>(new TriplePointComparer())
        {
            (tet.ABC.P0, tet.ABC.P1, tet.ABC.P2),
            (tet.ABD.P0, tet.ABD.P1, tet.ABD.P2),
            (tet.ACD.P0, tet.ACD.P1, tet.ACD.P2),
            (tet.BCD.P0, tet.BCD.P1, tet.BCD.P2)
        };

        Assert.Contains((a, b, c), faces);
        Assert.Contains((a, b, d), faces);
        Assert.Contains((a, c, d), faces);
        Assert.Contains((b, c, d), faces);
    }

    private static void AssertOutwardNormals(in Tetrahedron tet, double tol = 0)
    {
        // For each face, dot(normal, missing - p0) must be < 0
        Assert.True(DotToMissing(tet.ABC, tet.D) <= -tol);
        Assert.True(DotToMissing(tet.ABD, tet.C) <= -tol);
        Assert.True(DotToMissing(tet.ACD, tet.B) <= -tol);
        Assert.True(DotToMissing(tet.BCD, tet.A) <= -tol);
    }

    private static double DotToMissing(in Triangle tri, in Point missing)
    {
        var p0 = new RealVector(tri.P0.X, tri.P0.Y, tri.P0.Z);
        var pm = new RealVector(missing.X, missing.Y, missing.Z);
        return tri.Normal.Dot(pm - p0);
    }

    private static void AssertAllFacesOrthogonal(in Tetrahedron tet, double tol = 1e-12)
    {
        AssertOrthogonal(tet.ABC, tol);
        AssertOrthogonal(tet.ABD, tol);
        AssertOrthogonal(tet.ACD, tol);
        AssertOrthogonal(tet.BCD, tol);
    }

    private static void AssertOrthogonal(in Triangle tri, double tol)
    {
        var v0 = new RealVector(tri.P0.X, tri.P0.Y, tri.P0.Z);
        var e1 = new RealVector(tri.P1.X - tri.P0.X, tri.P1.Y - tri.P0.Y, tri.P1.Z - tri.P0.Z);
        var e2 = new RealVector(tri.P2.X - tri.P0.X, tri.P2.Y - tri.P0.Y, tri.P2.Z - tri.P0.Z);
        AssertInRange(Math.Abs(tri.Normal.Dot(e1)), 0, tol);
        AssertInRange(Math.Abs(tri.Normal.Dot(e2)), 0, tol);
    }

    private static void AssertNormalsEqual(in Tetrahedron t0, in Tetrahedron t1, double tol)
    {
        AssertNormalApprox(t0.ABC.Normal, t1.ABC.Normal.X, t1.ABC.Normal.Y, t1.ABC.Normal.Z, tol);
        AssertNormalApprox(t0.ABD.Normal, t1.ABD.Normal.X, t1.ABD.Normal.Y, t1.ABD.Normal.Z, tol);
        AssertNormalApprox(t0.ACD.Normal, t1.ACD.Normal.X, t1.ACD.Normal.Y, t1.ACD.Normal.Z, tol);
        AssertNormalApprox(t0.BCD.Normal, t1.BCD.Normal.X, t1.BCD.Normal.Y, t1.BCD.Normal.Z, tol);
    }

    private static void AssertFaceConsistency(in Tetrahedron tet, double tol = 1e-12)
    {
        CheckFace(tet.ABC, tet.D);
        CheckFace(tet.ABD, tet.C);
        CheckFace(tet.ACD, tet.B);
        CheckFace(tet.BCD, tet.A);
        void CheckFace(in Triangle tri, in Point missing)
        {
            // Use Int128 math (mirroring library) for robust normal orientation.
            Int128 e1x = (Int128)tri.P1.X - (Int128)tri.P0.X;
            Int128 e1y = (Int128)tri.P1.Y - (Int128)tri.P0.Y;
            Int128 e1z = (Int128)tri.P1.Z - (Int128)tri.P0.Z;
            Int128 e2x = (Int128)tri.P2.X - (Int128)tri.P0.X;
            Int128 e2y = (Int128)tri.P2.Y - (Int128)tri.P0.Y;
            Int128 e2z = (Int128)tri.P2.Z - (Int128)tri.P0.Z;

            Int128 cx = e1y * e2z - e1z * e2y;
            Int128 cy = e1z * e2x - e1x * e2z;
            Int128 cz = e1x * e2y - e1y * e2x;

            var n = new RealVector((double)cx, (double)cy, (double)cz);
            var expected = RealNormal.FromVector(n);

            Int128 mdx = (Int128)missing.X - (Int128)tri.P0.X;
            Int128 mdy = (Int128)missing.Y - (Int128)tri.P0.Y;
            Int128 mdz = (Int128)missing.Z - (Int128)tri.P0.Z;
            Int128 sign = cx * mdx + cy * mdy + cz * mdz;
            if (sign >= 0)
            {
                expected = RealNormal.FromVector(n * -1.0);
            }

            AssertNormalApprox(tri.Normal, expected.X, expected.Y, expected.Z, tol);
        }
    }

    private static Point Scale(Point p, long s) => new Point(p.X * s, p.Y * s, p.Z * s);

    private static Point RandPoint(Random rnd, int min, int max)
        => new Point(rnd.Next(min, max + 1), rnd.Next(min, max + 1), rnd.Next(min, max + 1));

    private sealed class TriplePointComparer : IEqualityComparer<(Point, Point, Point)>
    {
        public bool Equals((Point, Point, Point) x, (Point, Point, Point) y)
        {
            return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) && x.Item3.Equals(y.Item3);
        }

        public int GetHashCode((Point, Point, Point) obj)
        {
            unchecked
            {
                int h = 17;
                h = h * 23 + obj.Item1.GetHashCode();
                h = h * 23 + obj.Item2.GetHashCode();
                h = h * 23 + obj.Item3.GetHashCode();
                return h;
            }
        }
    }

    private static IEnumerable<Point[]> Permute(Point[] pts)
    {
        var arr = (Point[])pts.Clone();
        return PermuteImpl(arr, 0);
    }

    private static IEnumerable<Point[]> PermuteImpl(Point[] arr, int index)
    {
        if (index == arr.Length)
        {
            var copy = new Point[arr.Length];
            Array.Copy(arr, copy, arr.Length);
            yield return copy;
            yield break;
        }

        for (int i = index; i < arr.Length; i++)
        {
            (arr[index], arr[i]) = (arr[i], arr[index]);
            foreach (var p in PermuteImpl(arr, index + 1))
                yield return p;
            (arr[index], arr[i]) = (arr[i], arr[index]);
        }
    }

    [Fact]
    public void HardcodedNormals_UnitTetrahedron()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d = new Point(0, 0, 1);

        var tet = new Tetrahedron(a, b, c, d);

        // Expected outward normals:
        // ABC: plane z=0, missing D above -> outward -Z
        AssertNormalApprox(tet.ABC.Normal, 0, 0, -1, 1e-12);
        // ABD: plane y=0, missing C at +Y -> outward -Y
        AssertNormalApprox(tet.ABD.Normal, 0, -1, 0, 1e-12);
        // ACD: plane x=0, missing B at +X -> outward -X
        AssertNormalApprox(tet.ACD.Normal, -1, 0, 0, 1e-12);
        // BCD: skewed plane, outward along (1,1,1)/sqrt(3)
        var inv = 1.0 / Math.Sqrt(3.0);
        AssertNormalApprox(tet.BCD.Normal, inv, inv, inv, 1e-12);
    }

    private static void AssertNormalApprox(RealNormal n, double x, double y, double z, double tol)
    {
        AssertInRange(n.X, x, tol);
        AssertInRange(n.Y, y, tol);
        AssertInRange(n.Z, z, tol);
    }
}

