using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class DelaunayTriangulatorTests
    {
        [Fact]
        public void Run_Throws_OnNullPoints()
        {
            Assert.Throws<ArgumentNullException>(() => new Delaunay2DInput(points: null!, segments: Array.Empty<(int A, int B)>()));
        }

        [Fact]
        public void Run_Throws_OnTooFewPoints()
        {
            var input = new Delaunay2DInput(
                new List<RealPoint2D> { new RealPoint2D(0, 0), new RealPoint2D(1, 0) },
                Array.Empty<(int A, int B)>());

            var ex = Assert.Throws<ArgumentException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("at least 3 points", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Run_Throws_OnDuplicatePoints()
        {
            var input = new Delaunay2DInput(
                new List<RealPoint2D>
                {
                    new RealPoint2D(0, 0),
                    new RealPoint2D(1, 0),
                    new RealPoint2D(0, 0) // duplicate of first
                },
                Array.Empty<(int A, int B)>());

            var ex = Assert.Throws<ArgumentException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Run_Throws_OnCollinearPoints()
        {
            var input = new Delaunay2DInput(
                new List<RealPoint2D>
                {
                    new RealPoint2D(0, 0),
                    new RealPoint2D(1, 0),
                    new RealPoint2D(2, 0)
                },
                Array.Empty<(int A, int B)>());

            var ex = Assert.Throws<ArgumentException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("collinear", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Run_ProducesTriangles_ForSimpleTriangle()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(0, 1)
            };

            var input = new Delaunay2DInput(points, Array.Empty<(int A, int B)>());
            var result = Delaunay2DTriangulator.Run(in input);

            Assert.Single(result.Triangles);
            var triangle = result.Triangles[0];
            Assert.True(triangle.A < points.Count && triangle.B < points.Count && triangle.C < points.Count);
        }

        [Fact]
        public void Run_RespectsConstraintDiagonal()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(1, 1),
                new RealPoint2D(0, 1)
            };

            var segments = new List<(int A, int B)> { (0, 2) };
            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            bool hasDiagonal = false;
            foreach (var tri in result.Triangles)
            {
                if ((tri.A == 0 && tri.B == 2) || (tri.B == 0 && tri.C == 2) || (tri.C == 0 && tri.A == 2))
                {
                    hasDiagonal = true;
                    break;
                }
            }

            Assert.True(hasDiagonal, "Constraint diagonal (0,2) was not present in the triangulation.");
        }
    }
}
