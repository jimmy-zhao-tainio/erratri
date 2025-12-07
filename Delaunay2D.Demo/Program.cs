using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Versioning;
using Delaunay2D;
using Geometry;

namespace Delaunay2D.Demo
{
    internal static class Program
    {
        private const int CanvasSize = 800;
        private const int Margin = 40;

        [SupportedOSPlatform("windows")]
        private static void Main()
        {
            var points   = BuildPoints();
            var segments = BuildSegments(points.Count);

            var input  = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            var imagePath = Path.GetFullPath("erratriforce_circle.png");
            Render(result.Points, result.Triangles, segments, imagePath);

            Console.WriteLine($"Vertices:  {result.Points.Count}");
            Console.WriteLine($"Triangles: {result.Triangles.Count}");
            Console.WriteLine($"Image written to: {imagePath}");
        }

        private static List<RealPoint2D> BuildPoints()
        {
            var points = new List<RealPoint2D>
            {
                // Outer big triangle
                new RealPoint2D(0,   0),   // 0
                new RealPoint2D(100, 0),   // 1
                new RealPoint2D(50,  100), // 2
            };

            // Inner "circle" = regular 20-gon fully inside the outer triangle
            const int    innerCount = 20;
            const double centerX    = 50.0;
            const double centerY    = 45.0;
            const double radius     = 18.0;

            for (int i = 0; i < innerCount; i++)
            {
                double angle = 2.0 * Math.PI * i / innerCount;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);
                points.Add(new RealPoint2D(x, y));
            }

            return points;
        }

        private static List<(int A, int B)> BuildSegments(int pointCount)
        {
            var segments = new List<(int A, int B)>
            {
                // Outer triangle edges
                (0, 1),
                (1, 2),
                (2, 0)
            };

            // Inner 20-gon edges: indices 3 .. pointCount-1
            int firstInner = 3;
            int lastInner  = pointCount - 1;

            for (int i = firstInner; i <= lastInner; i++)
            {
                int j = (i == lastInner) ? firstInner : i + 1;
                segments.Add((i, j));
            }

            return segments;
        }

        private static void Render(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B, int C)> triangles,
            IReadOnlyList<(int A, int B)> constraints,
            string path)
        {
            // Bounding box
            double minX = points[0].X, maxX = points[0].X;
            double minY = points[0].Y, maxY = points[0].Y;
            for (int i = 1; i < points.Count; i++)
            {
                var p = points[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            double scale = (CanvasSize - 2 * Margin) / Math.Max(maxX - minX, maxY - minY);

            PointF Map(RealPoint2D p) => new(
                (float)(Margin + (p.X - minX) * scale),
                (float)(CanvasSize - Margin - (p.Y - minY) * scale)); // flip Y

            using var bmp = new Bitmap(CanvasSize, CanvasSize);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Black);

            var lineColor = Color.FromArgb(0xEE, 0xEE, 0xEE);

            // Triangles
            using (var triPen = new Pen(lineColor, 1f))
            {
                foreach (var tri in triangles)
                {
                    var a = Map(points[tri.A]);
                    var b = Map(points[tri.B]);
                    var c = Map(points[tri.C]);
                    g.DrawPolygon(triPen, new[] { a, b, c });
                }
            }

            // Constrained edges (same color, slightly thicker)
            using (var segPen = new Pen(lineColor, 2f))
            {
                foreach (var seg in constraints)
                {
                    g.DrawLine(segPen, Map(points[seg.A]), Map(points[seg.B]));
                }
            }

            // Vertices as small dots (same color)
            using (var brush = new SolidBrush(lineColor))
            {
                const float r = 2f;
                foreach (var pt in points)
                {
                    var p = Map(pt);
                    g.FillEllipse(brush, p.X - r, p.Y - r, r * 2, r * 2);
                }
            }

            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
