using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Versioning;
using Geometry;

namespace TriangleGarden.Demo
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

            var input  = new TriangleGardenInput(points, segments);
            var result = TriangleGardenTriangulator.Run(in input);

            var imagePath = Path.GetFullPath("triangle_garden.png");
            Render(result.Points, result.Triangles, segments, imagePath);

            Console.WriteLine($"Vertices:  {result.Points.Count}");
            Console.WriteLine($"Triangles: {result.Triangles.Count}");
            Console.WriteLine($"Image written to: {imagePath}");
        }

        private static List<RealPoint2D> BuildPoints()
        {
            // Base "house" shape in local coordinates, roughly [0,100] box.
            // Simple, non-self-intersecting polygon.
            var basePoly = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),    // bottom left
                new RealPoint2D(100, 0),  // bottom right
                new RealPoint2D(100, 60), // right wall top
                new RealPoint2D(50, 100), // roof peak
                new RealPoint2D(0, 60)    // left wall top
            };

            double cx = 0, cy = 0;
            foreach (var p in basePoly)
            {
                cx += p.X;
                cy += p.Y;
            }
            cx /= basePoly.Count;
            cy /= basePoly.Count;

            var points = new List<RealPoint2D>();

            const int    levels     = 6;
            const double scaleStep  = 0.75;

            for (int level = 0; level < levels; level++)
            {
                double s = Math.Pow(scaleStep, level);

                foreach (var p in basePoly)
                {
                    double x = cx + (p.X - cx) * s;
                    double y = cy + (p.Y - cy) * s;
                    points.Add(new RealPoint2D(x, y));
                }
            }

            return points;
        }

        private static List<(int A, int B)> BuildSegments(int pointCount)
        {
            var segments = new List<(int A, int B)>();

            const int baseVertexCount = 5;  // house polygon vertices
            int levels = pointCount / baseVertexCount;

            for (int level = 0; level < levels; level++)
            {
                int first = level * baseVertexCount;
                int last  = first + baseVertexCount - 1;

                for (int i = first; i <= last; i++)
                {
                    int j = (i == last) ? first : i + 1;
                    segments.Add((i, j));
                }
            }

            return segments;
        }

        private static void Render(
            IReadOnlyList<RealPoint2D> points,
            IReadOnlyList<(int A, int B, int C)> triangles,
            IReadOnlyList<(int A, int B)> constraints,
            string path)
        {
            // --- Compute bounds and mapping to canvas ---

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

            double spanX = maxX - minX;
            double spanY = maxY - minY;
            double scale = (CanvasSize - 2 * Margin) / Math.Max(spanX, spanY);

            PointF Map(RealPoint2D p) => new(
                (float)(Margin + (p.X - minX) * scale),
                (float)(CanvasSize - Margin - (p.Y - minY) * scale));

            double centerX = 0.5 * (minX + maxX);
            double centerY = 0.5 * (minY + maxY);

            using var bmp = new Bitmap(CanvasSize, CanvasSize);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Black);

            using var triStrokePen  = new Pen(Color.FromArgb(230, 255, 255, 255), 1.0f);
            using var constraintPen = new Pen(Color.FromArgb(255, 255, 255, 255), 2.0f);
            using var vertexBrush   = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

            double maxRadius = 0.0;
            foreach (var tri in triangles)
            {
                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];

                double cxTri = (pa.X + pb.X + pc.X) / 3.0;
                double cyTri = (pa.Y + pb.Y + pc.Y) / 3.0;
                double dx = cxTri - centerX;
                double dy = cyTri - centerY;
                double r  = Math.Sqrt(dx * dx + dy * dy);
                if (r > maxRadius) maxRadius = r;
            }
            if (maxRadius <= 0) maxRadius = 1.0;

            foreach (var tri in triangles)
            {
                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];

                double cxTri = (pa.X + pb.X + pc.X) / 3.0;
                double cyTri = (pa.Y + pb.Y + pc.Y) / 3.0;

                double dx = cxTri - centerX;
                double dy = cyTri - centerY;
                double r  = Math.Sqrt(dx * dx + dy * dy) / maxRadius;

                double brightness = 0.85 - 0.55 * r;
                brightness = Math.Clamp(brightness, 0.30, 0.90);

                var fillColor = FromGray(brightness);
                using var fillBrush = new SolidBrush(fillColor);

                var a = Map(pa);
                var b = Map(pb);
                var c = Map(pc);
                var poly = new[] { a, b, c };

                g.FillPolygon(fillBrush, poly);
                g.DrawPolygon(triStrokePen, poly);
            }

            foreach (var seg in constraints)
            {
                var p1 = Map(points[seg.A]);
                var p2 = Map(points[seg.B]);
                g.DrawLine(constraintPen, p1, p2);
            }

            const float rVertex = 2.5f;
            foreach (var pt in points)
            {
                var p = Map(pt);
                g.FillEllipse(vertexBrush, p.X - rVertex, p.Y - rVertex, rVertex * 2, rVertex * 2);
            }

            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }

        private static Color FromGray(double value, int alpha = 255)
        {
            value = Math.Clamp(value, 0.0, 1.0);
            byte c = (byte)Math.Round(value * 255.0);
            return Color.FromArgb(alpha, c, c, c);
        }
    }
}
