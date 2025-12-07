using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class PolygonTriangulatorWithHolesTests
    {
        [Fact]
        public void TriangulateWithHoles_AnnulusAreaMatches()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(3, 0),   // 1
                new RealPoint2D(3, 3),   // 2
                new RealPoint2D(0, 3),   // 3
                new RealPoint2D(1, 1),   // 4 inner
                new RealPoint2D(2, 1),   // 5 inner
                new RealPoint2D(2, 2),   // 6 inner
                new RealPoint2D(1, 2)    // 7 inner
            };

            int[] outer = { 0, 1, 2, 3 };
            var innerRings = new List<int[]> { new[] { 4, 5, 6, 7 } };

            var triangles = PolygonTriangulator2D.TriangulateWithHoles(outer, innerRings, points);

            Assert.NotEmpty(triangles);

            double PolygonArea(int[] ring)
            {
                double area2 = 0.0;
                for (int i = 0; i < ring.Length; i++)
                {
                    var p0 = points[ring[i]];
                    var p1 = points[ring[(i + 1) % ring.Length]];
                    area2 += p0.X * p1.Y - p1.X * p0.Y;
                }
                return 0.5 * area2;
            }

            double outerArea = Math.Abs(PolygonArea(outer));
            double innerArea = Math.Abs(PolygonArea(innerRings[0]));
            double expectedArea = outerArea - innerArea;

            double totalArea = 0.0;
            foreach (var tri in triangles)
            {
                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];
                totalArea += Math.Abs(0.5 * ((pb.X - pa.X) * (pc.Y - pa.Y) - (pb.Y - pa.Y) * (pc.X - pa.X)));
            }

            double diff = Math.Abs(totalArea - expectedArea);
            Assert.True(diff <= 0.6, $"Area mismatch: expected {expectedArea}, got {totalArea}, diff {diff}");

            // Ensure edges are manifold and no duplicate triangles.
            var edgeCounts = new Dictionary<(int, int), int>();
            var triKeys = new HashSet<(int, int, int)>();
            foreach (var tri in triangles)
            {
                int a = tri.A, b = tri.B, c = tri.C;
                var key = SortedTriKey(a, b, c);
                Assert.True(triKeys.Add(key), $"Duplicate triangle ({a},{b},{c})");

                Accumulate(edgeCounts, a, b);
                Accumulate(edgeCounts, b, c);
                Accumulate(edgeCounts, c, a);
            }

            foreach (var kvp in edgeCounts)
            {
                Assert.InRange(kvp.Value, 1, 2);
            }
        }

        [Fact]
        public void TriangulateWithHoles_SkewedInnerQuad()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(5, 0),   // 1
                new RealPoint2D(5, 4),   // 2
                new RealPoint2D(0, 4),   // 3
                new RealPoint2D(1.5, 1), // 4 inner skew
                new RealPoint2D(3.5, 0.5), // 5 inner skew
                new RealPoint2D(4, 2.5), // 6 inner skew
                new RealPoint2D(2, 3)    // 7 inner skew
            };

            int[] outer = { 0, 1, 2, 3 };
            var innerRings = new List<int[]> { new[] { 4, 5, 6, 7 } };

            var triangles = PolygonTriangulator2D.TriangulateWithHoles(outer, innerRings, points);

            Assert.NotEmpty(triangles);

            double PolygonArea(int[] ring)
            {
                double area2 = 0.0;
                for (int i = 0; i < ring.Length; i++)
                {
                    var p0 = points[ring[i]];
                    var p1 = points[ring[(i + 1) % ring.Length]];
                    area2 += p0.X * p1.Y - p1.X * p0.Y;
                }
                return 0.5 * area2;
            }

            double outerArea = Math.Abs(PolygonArea(outer));
            double innerArea = Math.Abs(PolygonArea(innerRings[0]));
            double expectedArea = outerArea - innerArea;

            double totalArea = 0.0;
            foreach (var tri in triangles)
            {
                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];
                totalArea += Math.Abs(0.5 * ((pb.X - pa.X) * (pc.Y - pa.Y) - (pb.Y - pa.Y) * (pc.X - pa.X)));
            }

            double diff = Math.Abs(totalArea - expectedArea);
            Assert.True(diff <= 2.5, $"Area mismatch: expected {expectedArea}, got {totalArea}, diff {diff}");
        }

        private static (int, int, int) SortedTriKey(int a, int b, int c)
        {
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);
            return (a, b, c);
        }

        private static void Accumulate(Dictionary<(int, int), int> counts, int a, int b)
        {
            if (a > b) (a, b) = (b, a);
            var key = (a, b);
            counts.TryGetValue(key, out int val);
            counts[key] = val + 1;
        }
    }
}
