using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class CorridorHoleTests
    {
        [Fact]
        public void CorridorWithInnerRing_SucceedsWithHoleTriangulation()
        {
            // Outer square with an inner square hole, triangulated as an annulus.
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

            // Triangulate the annulus manually (outer square minus inner square).
            var triangles = new List<Triangle2D>
            {
                new Triangle2D(0, 1, 5, points),
                new Triangle2D(0, 5, 4, points),
                new Triangle2D(1, 2, 6, points),
                new Triangle2D(1, 6, 5, points),
                new Triangle2D(2, 3, 7, points),
                new Triangle2D(2, 7, 6, points),
                new Triangle2D(3, 0, 4, points),
                new Triangle2D(3, 4, 7, points),
            };

            var boundary = new CorridorBoundary(
                outerRing: new[] { 0, 1, 2, 3 },
                innerRings: new List<int[]> { new[] { 4, 5, 6, 7 } });

            var bridged = PolygonTriangulator2D.TriangulateWithHoles(
                boundary.OuterRing,
                boundary.InnerRings,
                points);

            MeshValidator2D.ValidateLocalMesh(bridged, points, "CorridorWithInnerRing");

            bool CentroidInsideHole(Triangle2D t)
            {
                var pa = points[t.A];
                var pb = points[t.B];
                var pc = points[t.C];
                double cx = (pa.X + pb.X + pc.X) / 3.0;
                double cy = (pa.Y + pb.Y + pc.Y) / 3.0;
                return cx > 1.0 && cx < 2.0 && cy > 1.0 && cy < 2.0;
            }

            foreach (var tri in bridged)
            {
                Assert.False(CentroidInsideHole(tri), "Triangle centroid lies inside the hole.");
            }
        }
    }
}
