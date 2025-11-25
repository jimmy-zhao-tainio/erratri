using System;
using Geometry;
using System.IO;
using System.Linq;
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

    [Fact]
    public void FailingCase_TriangleIndex84_DumpPslg()
    {
        // Same captured inputs; dump PSLG faces for offline inspection.
        var tri = new Triangle(
            new Point(62, 162, 100),
            new Point(31, 168, 104),
            new Point(47, 152, 121),
            new Point(0, 0, 0));

        var points = new[]
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

        var segments = new[]
        {
            new TriangleSubdivision.IntersectionSegment(0, 1),
            new TriangleSubdivision.IntersectionSegment(1, 2)
        };

        PslgBuilder.Build(points, segments, out var vertices, out var edges);
        PslgBuilder.BuildHalfEdges(vertices, edges, out var halfEdges);
        var faces = PslgBuilder.ExtractFaces(vertices, halfEdges);

        Assert.Equal(6, vertices.Count);
        Assert.Equal(7, edges.Count);
        Assert.Equal(2, faces.Count);

        double totalArea = faces.Sum(f => Math.Abs(f.SignedAreaUV));
        Assert.InRange(totalArea, 0.5 - 1e-6, 0.5 + 1e-6);

        double minArea = faces.Min(f => Math.Abs(f.SignedAreaUV));
        double maxArea = faces.Max(f => Math.Abs(f.SignedAreaUV));
        Assert.InRange(minArea, 0.06, 0.07);      // expected ~0.0698
        Assert.InRange(maxArea, 0.43, 0.44);      // expected ~0.4301

        var path = "pslg_failure_dump.txt";
        using (var sw = new StreamWriter(path, append: false))
        {
            sw.WriteLine("Vertices:");
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                sw.WriteLine($"  {i}: ({v.X},{v.Y}) corner={v.IsTriangleCorner} idx={v.CornerIndex}");
            }

            sw.WriteLine("Edges:");
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                sw.WriteLine($"  {i}: {e.Start}->{e.End} boundary={e.IsBoundary}");
            }

            sw.WriteLine("Faces (area, outer vertices):");
            for (int i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                sw.WriteLine($"  {i}: area={f.SignedAreaUV} verts=[{string.Join(",", f.OuterVertices)}]");
            }
        }

        // Force current behavior: area selection must still throw here to flag the defect.
        Assert.Throws<InvalidOperationException>(() =>
            PslgBuilder.SelectInteriorFaces(faces, expectedTriangleArea: 0.5));
    }
}
