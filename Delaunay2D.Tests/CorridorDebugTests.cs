using System;
using System.Collections.Generic;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class CorridorDebugTests
    {
        [Fact]
        public void CorridorDumpHandler_IsInvokedOncePerConstraint()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(0, 1)
            };

            int dumpCount = 0;

            var debug = new Delaunay2DDebugOptions
            {
                EnableCorridorDump = true,
                CorridorDumpHandler = _ => dumpCount++
            };

            var input = new Delaunay2DInput(points, Array.Empty<(int A, int B)>())
            {
                Debug = debug
            };

            _ = Delaunay2DTriangulator.Run(in input);
            Assert.Equal(0, dumpCount);
        }
    }
}
