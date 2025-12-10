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

            // Second demo: star ring + center hub
            var starPoints   = BuildPointsStar();
            var starSegments = BuildSegmentsStar(starPoints.Count);
            var starInput    = new TriangleGardenInput(starPoints, starSegments);
            var starResult   = TriangleGardenTriangulator.Run(in starInput);

            var starImagePath = Path.GetFullPath("triangle_garden_star.png");
            Render(starResult.Points, starResult.Triangles, starSegments, starImagePath);

            Console.WriteLine($"[Star] Vertices:  {starResult.Points.Count}");
            Console.WriteLine($"[Star] Triangles: {starResult.Triangles.Count}");
            Console.WriteLine($"[Star] Image written to: {starImagePath}");
        }

        private static List<RealPoint2D> BuildPoints()
        {
            var pts = new List<RealPoint2D>();
            const double cx = 50.0;
            const double cy = 50.0;

            // OUTER RING (120-gon)
            const int outerN = 120;
            const double outerR = 45.0;

            for (int i = 0; i < outerN; i++)
            {
                double ang = 2 * Math.PI * i / outerN;
                pts.Add(new RealPoint2D(cx + outerR * Math.Cos(ang),
                                        cy + outerR * Math.Sin(ang)));
            }

            // INNER STAR (12-pt)
            const int starN = 12;
            const double r1 = 20.0;
            const double r2 = 8.0;

            for (int i = 0; i < starN; i++)
            {
                double ang = 2 * Math.PI * i / starN;
                double r = (i % 2 == 0) ? r1 : r2;
                pts.Add(new RealPoint2D(cx + r * Math.Cos(ang),
                                        cy + r * Math.Sin(ang)));
            }

            // CENTER
            pts.Add(new RealPoint2D(cx, cy));

            return pts;
        }

        private static List<(int A, int B)> BuildSegments(int pointCount)
        {
            var seg = new List<(int, int)>();

            const int outerN = 120;
            const int starN = 12;

            // Outer ring
            for (int i = 0; i < outerN; i++)
                seg.Add((i, (i + 1) % outerN));

            // Star cycle
            int starStart = outerN;
            for (int i = 0; i < starN; i++)
                seg.Add((starStart + i,
                         starStart + ((i + 1) % starN)));

            // Center connections (no cycle — just rays)
            int center = outerN + starN;

            seg.Add((center, 0));
            seg.Add((center, outerN / 3));
            seg.Add((center, (2 * outerN) / 3));

            return seg;
        }

        private static List<RealPoint2D> BuildPointsStar()
        {
            var points = new List<RealPoint2D>();

            const double cx = 50.0;
            const double cy = 50.0;

            const int    outerCount = 40;
            const double outerR     = 47.0;

            for (int i = 0; i < outerCount; i++)
            {
                double angle = 2.0 * Math.PI * i / outerCount;
                double x = cx + outerR * Math.Cos(angle);
                double y = cy + outerR * Math.Sin(angle);
                points.Add(new RealPoint2D(x, y));
            }

            const int    starCount   = 20;
            const double starROuter  = 25.0;
            const double starRInner  = 18.0;

            for (int i = 0; i < starCount; i++)
            {
                double baseAngle = 2.0 * Math.PI * i / starCount;
                double r = (i % 2 == 0) ? starROuter : starRInner;

                double x = cx + r * Math.Cos(baseAngle);
                double y = cy + r * Math.Sin(baseAngle);
                points.Add(new RealPoint2D(x, y));
            }

            points.Add(new RealPoint2D(cx, cy));

            return points;
        }

        private static List<(int A, int B)> BuildSegmentsStar(int pointCount)
        {
            var segments = new List<(int A, int B)>();

            const int outerCount = 40;
            const int starCount  = 20;

            int expectedPoints = outerCount + starCount + 1;
            if (pointCount != expectedPoints)
            {
                throw new InvalidOperationException($"Point count mismatch: expected {expectedPoints}, got {pointCount}");
            }

            int firstOuter = 0;
            int lastOuter  = outerCount - 1;
            for (int i = firstOuter; i <= lastOuter; i++)
            {
                int j = (i == lastOuter) ? firstOuter : i + 1;
                segments.Add((i, j));
            }

            int starFirst = outerCount;
            int starLast  = starFirst + starCount - 1;
            for (int i = starFirst; i <= starLast; i++)
            {
                int j = (i == starLast) ? starFirst : i + 1;
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
