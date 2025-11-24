using System;
using Geometry;
using Kernel;
using Xunit;

namespace Kernel.Tests;

public class TriangleSubdivisionDegenerateTests
{
    [Fact]
    public void FailingCase_TriangleIndex84_ReproThrows()
    {
        // Captured from Demo.Mesh triangleIndex=84 dump.
        var tri = new Triangle(
            new Point(62, 162, 100),
            new Point(31, 168, 104),
            new Point(47, 152, 121),
            new Point(0, 0, 0)); // missing point unused for subdivision

        var points = new []
        {
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.6822180488287042, 0.3177819511712962, -4.879217976976314E-16),
                new RealPoint(52.14875951368982, 163.90669170702776, 101.27112780468516)),
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.6319172558873369, 0.2082042891369133, 0.15987845497574965),
                new RealPoint(53.147490212119436, 161.65044118506395, 104.1902647110384)),
            new TriangleSubdivision.IntersectionPoint(
                new Barycentric(0.5732159406858199, -3.7949473154260213E-16, 0.42678405931418045),
                new RealPoint(55.5982391102873, 157.73215940685822, 108.96246524559778))
        };

        var segments = new []
        {
            new TriangleSubdivision.IntersectionSegment(0, 1),
            new TriangleSubdivision.IntersectionSegment(1, 2)
        };

        Assert.Throws<InvalidOperationException>(() =>
            TriangleSubdivision.Subdivide(in tri, points, segments));
    }
}
