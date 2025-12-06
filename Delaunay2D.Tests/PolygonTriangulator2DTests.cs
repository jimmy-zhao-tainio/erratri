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
    }
}
