using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Versioning;
using ConstrainedTriangulator;
using Geometry;

namespace Demo.Boolean.Triangulation.Triangulator
{
    internal static class Program
    {
        private const int CanvasSize = 800;
        private const int Margin = 40;
        private const double Foreshorten = 0.65;
        private const double OrbitTilt = 0.30;

        [SupportedOSPlatform("windows")]
        private static void Main()
        {
            var points      = BuildPoints();
            var constraints = BuildSegments(points.Count);
            var input       = new Input(points, constraints);

            //var slowResult = ConstrainedTriangulatorTriangulator.Run(in input, validate: true);
            //var slowPath   = Path.GetFullPath("constrained_triangulator_slow.png");
            //Render(slowResult.Points, slowResult.Triangles, constraints, slowPath);

            var fastResult = ConstrainedTriangulator.Triangulator.RunFast(in input, validate: true);
            var fastPath   = Path.GetFullPath("constrained_triangulator_fast.png");
            Render(fastResult.Points, fastResult.Triangles, constraints, fastPath);

            //Console.WriteLine("Slow triangulation:");
            //Console.WriteLine($"  Vertices:  {slowResult.Points.Count}");
            //Console.WriteLine($"  Triangles: {slowResult.Triangles.Count}");
            //Console.WriteLine($"  Image:     {slowPath}");
            //Console.WriteLine();
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

            const double sunScreenRadius = 5.0;
            double sunRx = sunScreenRadius;
            double sunRy = sunScreenRadius / Foreshorten;

            var rings = new (int count, double rx, double ry, double rot)[]
            {
                (180, 52.0, 24.0, OrbitTilt),
                (160, 46.0, 21.0, OrbitTilt),
                (140, 40.0, 18.0, OrbitTilt),
                (120, 34.0, 15.0, OrbitTilt),
                (100, 28.0, 12.0, OrbitTilt),
                ( 80, 22.0,  9.0, OrbitTilt),
                ( 48, sunRx, sunRy, 0.0)
            };

            foreach (var r in rings)
            {
                AddEllipse(r.count, cx, cy, r.rx, r.ry, r.rot);
            }

            return points;
        }

        private static List<(int A, int B)> BuildSegments(int pointCount)
        {
            var segments = new List<(int A, int B)>();

            int[] ringCounts = { 180, 160, 140, 120, 100, 80, 48 };

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

            double centerX = 0.5 * (minX + maxX);
            double centerY = 0.5 * (minY + maxY);
            double halfX = 0.5 * (maxX - minX);
            double halfY = 0.5 * (maxY - minY);

            double effectiveHalf = Math.Max(halfX, halfY * Foreshorten);
            double scale = (CanvasSize - 2 * Margin) / (2.0 * effectiveHalf);

            const double verticalCenterBias = 0.0;

            PointF Map(RealPoint2D p) => new(
                (float)(CanvasSize * 0.5 + (p.X - centerX) * scale),
                (float)(CanvasSize * (0.5 - verticalCenterBias) - (p.Y - centerY) * scale * Foreshorten));

            double maxRadius = 0.0;
            foreach (var pt in points)
            {
                double dx = pt.X - centerX;
                double dy = pt.Y - centerY;
                double r = Math.Sqrt(dx * dx + dy * dy);
                if (r > maxRadius) maxRadius = r;
            }
            if (maxRadius <= 0) maxRadius = 1.0;

            using var bmp = new Bitmap(CanvasSize, CanvasSize);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Black);

            using var triStrokePen  = new Pen(Color.FromArgb(60, 255, 255, 255), 0.5f);
            using var triWirePen    = new Pen(Color.FromArgb(40, 255, 255, 255), 0.4f);
            using var constraintPen = new Pen(Color.FromArgb(80, 220, 220, 220), 1.0f);
            using var vertexBrush   = new SolidBrush(Color.FromArgb(0, 255, 255, 255));

            double rx = halfX;
            double ry = halfY;
            var lightDir = Normalize3(0.4, 0.6, 1.0);

            foreach (var tri in triangles)
            {
                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];

                double cxTri = (pa.X + pb.X + pc.X) / 3.0;
                double cyTri = (pa.Y + pb.Y + pc.Y) / 3.0;

                double ux = (cxTri - centerX) / rx;
                double uy = (cyTri - centerY) / ry;
                double r2 = ux * ux + uy * uy;

                double uz = 0.0;
                if (r2 <= 1.0)
                {
                    uz = Math.Sqrt(1.0 - r2);
                }

                var normal = Normalize3(ux, uy, uz);

                double ambient = 0.40;
                double diffuse = 0.85 * Math.Max(0.0, Dot3(normal, lightDir));
                double brightness = ambient + diffuse;
                double dx = cxTri - centerX;
                double dy = cyTri - centerY;
                double rNorm = Math.Sqrt(dx * dx + dy * dy) / maxRadius;
                brightness *= (1.0 - 0.25 * rNorm);
                brightness = Math.Clamp(brightness, 0.0, 1.0);

                var a = Map(pa);
                var b = Map(pb);
                var c = Map(pc);
                var poly = new[] { a, b, c };

                g.DrawPolygon(triWirePen, poly);
            }

            foreach (var seg in constraints)
            {
                var p1 = Map(points[seg.A]);
                var p2 = Map(points[seg.B]);
                g.DrawLine(constraintPen, p1, p2);
            }

            // Vertices omitted for a cleaner shaded look
            // const float rVertex = 2.5f;
            // foreach (var pt in points)
            // {
            //     var p = Map(pt);
            //     g.FillEllipse(vertexBrush, p.X - rVertex, p.Y - rVertex, rVertex * 2, rVertex * 2);
            // }

            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);

            static (double x, double y, double z) Normalize3(double x, double y, double z)
            {
                double len = Math.Sqrt(x * x + y * y + z * z);
                if (len <= 0.0) return (0.0, 0.0, 0.0);
                return (x / len, y / len, z / len);
            }

            static double Dot3((double x, double y, double z) a, (double x, double y, double z) b)
                => a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private static Color FromGray(double value, int alpha = 255)
        {
            value = Math.Clamp(value, 0.0, 1.0);
            byte c = (byte)Math.Round(value * 255.0);
            return Color.FromArgb(alpha, c, c, c);
        }
    }
}
