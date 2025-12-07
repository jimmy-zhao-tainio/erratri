using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class LocalDelaunayRelaxationTests
    {
        [Fact]
        public void Relax_FlipsNonDelaunayEdge()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(10, 0),  // 1
                new RealPoint2D(10, 0.2),// 2
                new RealPoint2D(0, 1)    // 3
            };

            var triangles = new List<Triangle2D>
            {
                new Triangle2D(0, 1, 3, points), // diagonal 1-3
                new Triangle2D(1, 2, 3, points)
            };

            var patch = new List<int> { 0, 1 };
            var constrained = new HashSet<Edge2D>();

            LocalDelaunayRelaxation.Relax(points, triangles, patch, constrained, log: false);

            // After relaxation, the diagonal should be 0-2 instead of 1-3.
            bool hasNewDiag = Geometry2DIntersections.TriangleHasUndirectedEdge(triangles[0], 0, 2) ||
                              Geometry2DIntersections.TriangleHasUndirectedEdge(triangles[1], 0, 2);
            bool stillHasOldDiag = Geometry2DIntersections.TriangleHasUndirectedEdge(triangles[0], 1, 3) ||
                                   Geometry2DIntersections.TriangleHasUndirectedEdge(triangles[1], 1, 3);

            Assert.True(hasNewDiag, "Expected edge (0,2) after relaxation.");
            Assert.False(stillHasOldDiag, "Edge (1,3) should have been flipped away.");
        }

        [Fact]
        public void Relax_DoesNotFlipConstrainedEdge()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(10, 0),  // 1
                new RealPoint2D(10, 0.2),// 2
                new RealPoint2D(0, 1)    // 3
            };

            var triangles = new List<Triangle2D>
            {
                new Triangle2D(0, 1, 3, points), // diagonal 1-3
                new Triangle2D(1, 2, 3, points)
            };

            var patch = new List<int> { 0, 1 };
            var constrained = new HashSet<Edge2D> { new Edge2D(1, 3) };

            LocalDelaunayRelaxation.Relax(points, triangles, patch, constrained, log: false);

            // Constrained diagonal should remain.
            bool stillHasOldDiag = Geometry2DIntersections.TriangleHasUndirectedEdge(triangles[0], 1, 3) ||
                                   Geometry2DIntersections.TriangleHasUndirectedEdge(triangles[1], 1, 3);
            Assert.True(stillHasOldDiag, "Constrained edge (1,3) must not be flipped.");
        }
    }
}
