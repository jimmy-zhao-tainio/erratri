using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class ConstraintUnsupportedTests
    {
        [Fact]
        public void CrossingConstraints_Throws()
        {
            // Square with crossing diagonals as constraints.
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0), // 0
                new RealPoint2D(1, 0), // 1
                new RealPoint2D(1, 1), // 2
                new RealPoint2D(0, 1)  // 3
            };

            var segments = new List<(int A, int B)> { (0, 2), (1, 3) };
            var input = new Delaunay2DInput(points, segments);

            var ex = Assert.Throws<InvalidOperationException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("degree-2", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void InnerLoopConstraints_Respected()
        {
            // Outer triangle with an inner triangle loop as constraints.
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0 outer
                new RealPoint2D(5, 0),   // 1 outer
                new RealPoint2D(0, 5),   // 2 outer
                new RealPoint2D(1, 1),   // 3 inner
                new RealPoint2D(2, 1),   // 4 inner
                new RealPoint2D(1, 2)    // 5 inner
            };

            var segments = new List<(int A, int B)>
            {
                (3, 4), (4, 5), (5, 3) // inner loop only
            };

            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Triangles);

            bool HasEdge((int A, int B, int C) tri, int a, int b)
            {
                return (tri.A == a && tri.B == b) ||
                       (tri.B == a && tri.C == b) ||
                       (tri.C == a && tri.A == b) ||
                       (tri.A == b && tri.B == a) ||
                       (tri.B == b && tri.C == a) ||
                       (tri.C == b && tri.A == a);
            }

            bool has34 = false, has45 = false, has53 = false;
            foreach (var tri in result.Triangles)
            {
                if (HasEdge(tri, 3, 4)) has34 = true;
                if (HasEdge(tri, 4, 5)) has45 = true;
                if (HasEdge(tri, 5, 3)) has53 = true;
            }

            Assert.True(has34 && has45 && has53, "Inner loop constraint edges were not all present in the triangulation.");
        }
    }
}
