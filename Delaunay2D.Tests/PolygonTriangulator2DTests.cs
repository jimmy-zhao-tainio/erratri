using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class PolygonTriangulator2DTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConvexQuad_TriangulatesToTwoTriangles(bool closed)
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(2, 0),
                new RealPoint2D(2, 1),
                new RealPoint2D(0, 1)
            };

            var ring = closed
                ? new List<int> { 0, 1, 2, 3, 0 }
                : new List<int> { 0, 1, 2, 3 };

            var triangles = PolygonTriangulator2D.TriangulateSimpleRing(ring, points);
            Assert.Equal(2, triangles.Count);

            double polygonArea = 2.0; // rectangle 2x1
            double triangleAreaSum = 0.0;
            foreach (var triangle in triangles)
            {
                var pa = points[triangle.A];
                var pb = points[triangle.B];
                var pc = points[triangle.C];
                double area = 0.5 * ((pb.X - pa.X) * (pc.Y - pa.Y) - (pb.Y - pa.Y) * (pc.X - pa.X));
                triangleAreaSum += Math.Abs(area);
            }

            double diff = Math.Abs(triangleAreaSum - polygonArea);
            Assert.True(diff <= 1e-9, $"Area mismatch: {triangleAreaSum} vs {polygonArea}");
        }

        [Fact]
        public void ConcavePentagon_Triangulates()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(2, 0),
                new RealPoint2D(1, 0.5),
                new RealPoint2D(2, 2),
                new RealPoint2D(0, 2)
            };

            var ring = new List<int> { 0, 1, 2, 3, 4 };

            var triangles = PolygonTriangulator2D.TriangulateSimpleRing(ring, points);
            Assert.Equal(ring.Count - 2, triangles.Count);
        }

        [Fact]
        public void RingWithDuplicateIndex_Throws()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(1, 1),
                new RealPoint2D(0, 1)
            };

            var ring = new List<int> { 0, 1, 2, 1 };

            Assert.Throws<InvalidOperationException>(() => PolygonTriangulator2D.TriangulateSimpleRing(ring, points));
        }

        [Fact]
        public void DegenerateArea_Throws()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(2, 0)
            };

            var ring = new List<int> { 0, 1, 2 };

            Assert.Throws<InvalidOperationException>(() => PolygonTriangulator2D.TriangulateSimpleRing(ring, points));
        }

        [Fact]
        public void SelfIntersectingRing_Throws()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 1),
                new RealPoint2D(0, 1),
                new RealPoint2D(1, 0)
            };

            var ring = new List<int> { 0, 1, 2, 3 };

            Assert.Throws<InvalidOperationException>(() => PolygonTriangulator2D.TriangulateSimpleRing(ring, points));
        }

        [Fact]
        public void NonDegenerateSelfIntersectingRing_Throws()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(4, 0),   // 1
                new RealPoint2D(5, 3),   // 2
                new RealPoint2D(2, 5),   // 3
                new RealPoint2D(-1, 3)   // 4
            };

            // Star order creates non-adjacent edge crossings.
            var ring = new List<int> { 0, 2, 4, 1, 3 };

            // Sanity: area should be non-zero so we fail on self-intersection, not degeneracy.
            double area = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                var p0 = points[ring[i]];
                var p1 = points[ring[(i + 1) % ring.Count]];
                area += p0.X * p1.Y - p1.X * p0.Y;
            }
            area *= 0.5;
            Assert.True(Math.Abs(area) > 1e-6);

            var ex = Assert.Throws<InvalidOperationException>(() => PolygonTriangulator2D.TriangulateSimpleRing(ring, points));
            Assert.Contains("self-intersecting", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
