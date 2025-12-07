using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class BowyerWatsonRobustnessTests
    {
        [Fact]
        public void BowyerWatson_Handles_NestedTwentyGonInsideTriangle()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0,   0),
                new RealPoint2D(100, 0),
                new RealPoint2D(50,  100),
            };

            const int innerCount = 20;
            const double cx = 50.0;
            const double cy = 45.0;
            const double r = 18.0;

            for (int i = 0; i < innerCount; i++)
            {
                double angle = 2.0 * Math.PI * i / innerCount;
                points.Add(new RealPoint2D(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)));
            }

            var triangles = BowyerWatsonTriangulator.Triangulate(points);

            Assert.NotNull(triangles);
            Assert.NotEmpty(triangles);
        }
    }
}
