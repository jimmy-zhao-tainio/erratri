using Geometry;
using Kernel;
using Xunit;

namespace Kernel.Tests;

public class PslgCheeseRegressionTests
{
    [Fact]
    public void Pslg_TriangulateSimple_CheesePolygon_ShouldProduceTriangles()
    {
        // This polygon + vertex set was dumped from a failing boolean op
        // (BoxMinusSingleCylinder). Current behavior: TriangulateSimple throws
        // "Ear clipping failed: no valid ear found for a non-triangular polygon."
        //
        // That is a BUG: this polygon should be triangulable.
        // This test encodes the desired behavior and is expected to FAIL until the bug is fixed.

        int[] polygon = { 1, 4, 3, 5, 2 };

        RealPoint[] vertices = new RealPoint[]
        {
            new RealPoint(1.0, 0.0, 0.0),                // 0
            new RealPoint(0.0, 1.0, 0.0),                // 1
            new RealPoint(0.0, 0.0, 0.0),                // 2
            new RealPoint(0.0, 0.7233333333333334, 0.0), // 3
            new RealPoint(0.0, 0.8333333333333334, 0.0), // 4
            new RealPoint(0.0, 0.16666666666666669, 0.0) // 5
        };

        double expectedArea = 0.5;

        // Act – current implementation throws here; that is the bug.
        // Build PslgVertex list from the RealPoint array (z is always 0 in this dump).
        var pslgVertices = new List<PslgVertex>(vertices.Length);
        foreach (var v in vertices)
        {
            pslgVertices.Add(new PslgVertex(v.X, v.Y, isTriangleCorner: false, cornerIndex: -1));
        }

        var triangles = PslgBuilder.TriangulateSimple(polygon, pslgVertices, expectedArea);

        // Assert – once fixed, we expect at least one triangle and no exception.
        Assert.NotNull(triangles);
        Assert.NotEmpty(triangles);
    }
}
