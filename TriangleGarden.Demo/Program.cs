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
            var points      = BuildPoints();
            var constraints = BuildSegments(points.Count);
            var input       = new TriangleGardenInput(points, constraints);

            var slowResult = TriangleGardenTriangulator.Run(in input, validate: true);
            var slowPath   = Path.GetFullPath("triangle_garden_slow.png");
            Render(slowResult.Points, slowResult.Triangles, constraints, slowPath);

            var fastResult = TriangleGardenTriangulator.RunFast(in input, validate: true);
            var fastPath   = Path.GetFullPath("triangle_garden_fast.png");
            Render(fastResult.Points, fastResult.Triangles, constraints, fastPath);

            Console.WriteLine("Slow triangulation:");
            Console.WriteLine($"  Vertices:  {slowResult.Points.Count}");
            Console.WriteLine($"  Triangles: {slowResult.Triangles.Count}");
            Console.WriteLine($"  Image:     {slowPath}");
            Console.WriteLine();
            Console.WriteLine("Fast triangulation:");
            Console.WriteLine($"  Vertices:  {fastResult.Points.Count}");
            Console.WriteLine($"  Triangles: {fastResult.Triangles.Count}");
            Console.WriteLine($"  Image:     {fastPath}");
        }

        private static List<RealPoint2D> BuildPoints()
        {
            var points = new List<RealPoint2D>();

            void AddEllipse(int count, double cx, double cy, double rx, double ry, double rotation)
            {
                double cosR = Math.Cos(rotation);
                double sinR = Math.Sin(rotation);

                for (int i = 0; i < count; i++)
                {
                    double t = 2.0 * Math.PI * i / count;
                    double ex = rx * Math.Cos(t);
                    double ey = ry * Math.Sin(t);

                    double xr = ex * cosR - ey * sinR;
                    double yr = ex * sinR + ey * cosR;

                    points.Add(new RealPoint2D(cx + xr, cy + yr));
                }
            }

            const double cx = 0.0;
            const double cy = 0.0;
            const double tilt = 0.25;

            AddEllipse(count: 140, cx: cx, cy: cy, rx: 48.0, ry: 32.0, rotation: tilt);
            AddEllipse(count: 110, cx: cx, cy: cy, rx: 36.0, ry: 24.0, rotation: tilt);
            AddEllipse(count: 80, cx: cx, cy: cy, rx: 26.0, ry: 17.0, rotation: tilt);
            AddEllipse(count: 50, cx: cx, cy: cy, rx: 17.0, ry: 11.0, rotation: tilt);
            AddEllipse(count: 32, cx: cx, cy: cy, rx: 6.0, ry: 6.0, rotation: 0.0);

            return points;
        }

        private static List<(int A, int B)> BuildSegments(int pointCount)
        {
            var segments = new List<(int A, int B)>();

            int[] ringCounts = { 140, 110, 80, 50, 32 };

            int offset = 0;
            foreach (int count in ringCounts)
            {
                int first = offset;
                int last = offset + count - 1;

                for (int i = first; i <= last; i++)
                {
                    int j = (i == last) ? first : i + 1;
                    segments.Add((i, j));
                }

                offset += count;
            }

            if (offset != pointCount)
            {
                throw new InvalidOperationException($"Point count mismatch: expected {offset}, got {pointCount}.");
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
