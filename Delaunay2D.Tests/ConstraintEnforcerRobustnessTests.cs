using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class ConstraintEnforcerRobustnessTests
    {
        [Fact]
        public void ConstraintEnforcer_Handles_TwentyGonLoopInsideTriangle()
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

            var segments = new List<(int A, int B)>
            {
                (0, 1), (1, 2), (2, 0)
            };

            int firstInner = 3;
            int lastInner = points.Count - 1;
            for (int i = firstInner; i <= lastInner; i++)
            {
                int j = (i == lastInner) ? firstInner : i + 1;
                segments.Add((i, j));
            }

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

            foreach (var seg in segments)
            {
                bool found = false;
                foreach (var tri in result.Triangles)
                {
                    if (HasEdge(tri, seg.A, seg.B))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found, $"Segment ({seg.A},{seg.B}) missing from triangulation.");
            }
        }
    }
}
