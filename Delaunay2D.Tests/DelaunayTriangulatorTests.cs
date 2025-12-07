using System;
using System.Collections.Generic;
using System.Linq;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class DelaunayTriangulatorTests
    {
        [Fact]
        public void Run_Throws_OnNullPoints()
        {
            Assert.Throws<ArgumentNullException>(() => new Delaunay2DInput(points: null!, segments: Array.Empty<(int A, int B)>()));
        }

        [Fact]
        public void Run_Throws_OnTooFewPoints()
        {
            var input = new Delaunay2DInput(
                new List<RealPoint2D> { new RealPoint2D(0, 0), new RealPoint2D(1, 0) },
                Array.Empty<(int A, int B)>());

            var ex = Assert.Throws<ArgumentException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("at least 3 points", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Run_Throws_OnDuplicatePoints()
        {
            var input = new Delaunay2DInput(
                new List<RealPoint2D>
                {
                    new RealPoint2D(0, 0),
                    new RealPoint2D(1, 0),
                    new RealPoint2D(0, 0) // duplicate of first
                },
                Array.Empty<(int A, int B)>());

            var ex = Assert.Throws<ArgumentException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Run_Throws_OnCollinearPoints()
        {
            var input = new Delaunay2DInput(
                new List<RealPoint2D>
                {
                    new RealPoint2D(0, 0),
                    new RealPoint2D(1, 0),
                    new RealPoint2D(2, 0)
                },
                Array.Empty<(int A, int B)>());

            var ex = Assert.Throws<ArgumentException>(() => Delaunay2DTriangulator.Run(in input));
            Assert.Contains("collinear", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Run_ProducesTriangles_ForSimpleTriangle()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(0, 1)
            };

            var input = new Delaunay2DInput(points, Array.Empty<(int A, int B)>());
            var result = Delaunay2DTriangulator.Run(in input);

            Assert.Single(result.Triangles);
            var triangle = result.Triangles[0];
            Assert.True(triangle.A < points.Count && triangle.B < points.Count && triangle.C < points.Count);
        }

        [Fact]
        public void Run_RespectsConstraintDiagonal()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(1, 1),
                new RealPoint2D(0, 1)
            };

            var segments = new List<(int A, int B)> { (0, 2) };
            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            bool hasDiagonal = false;
            foreach (var tri in result.Triangles)
            {
                if ((tri.A == 0 && tri.B == 2) ||
                    (tri.B == 0 && tri.C == 2) ||
                    (tri.C == 0 && tri.A == 2) ||
                    (tri.A == 2 && tri.B == 0) ||
                    (tri.B == 2 && tri.C == 0) ||
                    (tri.C == 2 && tri.A == 0))
                {
                    hasDiagonal = true;
                    break;
                }
            }

            Assert.True(hasDiagonal, "Constraint diagonal (0,2) was not present in the triangulation.");
        }

        [Fact]
        public void Run_RespectsHullEdgeConstraint()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(1, 1),
                new RealPoint2D(0, 1)
            };

            var segments = new List<(int A, int B)> { (0, 1) };
            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            bool hasEdge = false;
            foreach (var tri in result.Triangles)
            {
                if ((tri.A == 0 && tri.B == 1) ||
                    (tri.B == 0 && tri.C == 1) ||
                    (tri.C == 0 && tri.A == 1) ||
                    (tri.A == 1 && tri.B == 0) ||
                    (tri.B == 1 && tri.C == 0) ||
                    (tri.C == 1 && tri.A == 0))
                {
                    hasEdge = true;
                    break;
                }
            }

            Assert.True(hasEdge, "Constraint edge (0,1) was not present in the triangulation.");
        }

        [Fact]
        public void ThinConvexPolygon_TriangulatesWithCorrectArea()
        {
            // Very thin convex quad: height is tiny but non-zero.
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(10, 0.0005),
                new RealPoint2D(10, 0.001),
                new RealPoint2D(0, 0.0005)
            };

            var input = new Delaunay2DInput(points, Array.Empty<(int A, int B)>());
            var result = Delaunay2DTriangulator.Run(in input);

            Assert.NotEmpty(result.Triangles);

            static double TriangleArea2D(RealPoint2D a, RealPoint2D b, RealPoint2D c)
            {
                return 0.5 * ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
            }

            double polygonArea = 0.0;
            for (int i = 0; i < points.Count; i++)
            {
                var p0 = points[i];
                var p1 = points[(i + 1) % points.Count];
                polygonArea += p0.X * p1.Y - p1.X * p0.Y;
            }
            polygonArea = Math.Abs(0.5 * polygonArea);

            double totalArea = 0.0;
            foreach (var tri in result.Triangles)
            {
                var pa = points[tri.A];
                var pb = points[tri.B];
                var pc = points[tri.C];
                totalArea += Math.Abs(TriangleArea2D(pa, pb, pc));
            }

            double diff = Math.Abs(totalArea - polygonArea);
            Assert.True(diff <= 1e-9, $"Area mismatch: expected {polygonArea}, got {totalArea}, diff {diff}");
        }

        [Fact]
        public void Run_RespectsMultipleConstraintsSharingEndpoint()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(2, 0),
                new RealPoint2D(2, 1),
                new RealPoint2D(0, 1)
            };

            var segments = new List<(int A, int B)> { (0, 2), (2, 3) };
            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            bool has02 = false;
            bool has23 = false;
            foreach (var tri in result.Triangles)
            {
                if ((tri.A == 0 && tri.B == 2) ||
                    (tri.B == 0 && tri.C == 2) ||
                    (tri.C == 0 && tri.A == 2) ||
                    (tri.A == 2 && tri.B == 0) ||
                    (tri.B == 2 && tri.C == 0) ||
                    (tri.C == 2 && tri.A == 0))
                {
                    has02 = true;
                }

                if ((tri.A == 2 && tri.B == 3) ||
                    (tri.B == 2 && tri.C == 3) ||
                    (tri.C == 2 && tri.A == 3) ||
                    (tri.A == 3 && tri.B == 2) ||
                    (tri.B == 3 && tri.C == 2) ||
                    (tri.C == 3 && tri.A == 2))
                {
                    has23 = true;
                }
            }

            Assert.True(has02, "Constrained edge (0,2) not found in final triangulation.");
            Assert.True(has23, "Constrained edge (2,3) not found in final triangulation.");
        }

        [Fact]
        public void Constraint_CollinearChainOnly_Throws()
        {
            // Points forming a rectangle with an extra point on the bottom edge.
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(2, 0),   // 1
                new RealPoint2D(2, 1),   // 2
                new RealPoint2D(0, 1),   // 3
                new RealPoint2D(1, 0)    // 4 (midpoint on bottom edge)
            };

            // Constrain along the bottom edge chain (0 -> 2) which is collinear with existing edges but not present as a single edge.
            var segments = new List<(int A, int B)> { (0, 2) };
            var input = new Delaunay2DInput(points, segments);

            Assert.Throws<InvalidOperationException>(() => Delaunay2DTriangulator.Run(in input));
        }

        [Fact]
        public void Constraint_TouchOnlyAtVertex_Succeeds()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(2, 0),   // 1
                new RealPoint2D(2, 1),   // 2
                new RealPoint2D(0, 1)    // 3
            };

            // Constraint touches vertex 0 but otherwise lies along the hull edge; edge-exists shortcut should handle it without corridor.
            var segments = new List<(int A, int B)> { (0, 1) };
            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            bool hasEdge = false;
            foreach (var tri in result.Triangles)
            {
                if ((tri.A == 0 && tri.B == 1) ||
                    (tri.B == 0 && tri.C == 1) ||
                    (tri.C == 0 && tri.A == 1) ||
                    (tri.A == 1 && tri.B == 0) ||
                    (tri.B == 1 && tri.C == 0) ||
                    (tri.C == 1 && tri.A == 0))
                {
                    hasEdge = true;
                    break;
                }
            }

            Assert.True(hasEdge, "Constraint edge (0,1) was not present in the triangulation.");
        }

        [Fact]
        public void MixedInteriorAndCollinearOverlapConstraint_Succeeds()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(1, 0),   // 1
                new RealPoint2D(2, 0),   // 2
                new RealPoint2D(3, 0),   // 3
                new RealPoint2D(0, 2),   // 4
                new RealPoint2D(3, 2)    // 5
            };

            // Constrain from top-left to top-right across the strip; it will cross interior and overlap collinearly with bottom edges.
            var segments = new List<(int A, int B)> { (4, 2) }; // from (0,2) to (2,0): crosses, and overlaps base edges partly
            var input = new Delaunay2DInput(points, segments);
            var result = Delaunay2DTriangulator.Run(in input);

            bool hasEdge = false;
            foreach (var tri in result.Triangles)
            {
                if (Geometry2DIntersections.TriangleHasUndirectedEdge(
                        new Triangle2D(tri.A, tri.B, tri.C, points),
                        4, 2))
                {
                    hasEdge = true;
                    break;
                }
            }

            Assert.True(hasEdge, "Constraint edge (4,2) was not present in the triangulation.");
        }

        [Fact]
        public void PureCollinearConstraint_ThrowsCollinearChain()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0
                new RealPoint2D(1, 0),   // 1
                new RealPoint2D(2, 0),   // 2
                new RealPoint2D(3, 0),   // 3
                new RealPoint2D(0, 1),   // 4
                new RealPoint2D(3, 1)    // 5
            };

            // Constrain along the bottom chain but spanning multiple edges (not an existing single edge).
            var segments = new List<(int A, int B)> { (0, 3) };
            var input = new Delaunay2DInput(points, segments);

            Assert.Throws<InvalidOperationException>(() => Delaunay2DTriangulator.Run(in input));
        }

        [Fact]
        public void TouchOnlyConstraint_SucceedsWithoutCorridor()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),    // 0 hull vertex
                new RealPoint2D(2, 0),    // 1 hull vertex
                new RealPoint2D(1, 1),    // 2 hull vertex
                new RealPoint2D(-1, 0.5)  // 3 hull vertex; (0,3) is a hull edge
            };

            var unconstrainedInput = new Delaunay2DInput(points, Array.Empty<(int A, int B)>());
            var unconstrainedResult = Delaunay2DTriangulator.Run(in unconstrainedInput);

            var segments = new List<(int A, int B)> { (0, 3) };
            var constrainedInput = new Delaunay2DInput(points, segments);
            var constrainedResult = Delaunay2DTriangulator.Run(in constrainedInput);

            HashSet<(int, int, int)> ToTriangleKeySet(IReadOnlyList<(int A, int B, int C)> triangles)
            {
                var set = new HashSet<(int, int, int)>();
                foreach (var tri in triangles)
                {
                    int a = tri.A;
                    int b = tri.B;
                    int c = tri.C;
                    if (a > b) (a, b) = (b, a);
                    if (b > c) (b, c) = (c, b);
                    if (a > b) (a, b) = (b, a);
                    set.Add((a, b, c));
                }

                return set;
            }

            var unconstrainedKeys = ToTriangleKeySet(unconstrainedResult.Triangles);
            var constrainedKeys = ToTriangleKeySet(constrainedResult.Triangles);

            Assert.Equal(unconstrainedKeys.Count, constrainedKeys.Count);
            Assert.Subset(unconstrainedKeys, constrainedKeys);
            Assert.Subset(constrainedKeys, unconstrainedKeys);
        }

        [Fact]
        public void EndpointOnEdgeConstraint_Succeeds()
        {
            // Square with a midpoint on the bottom edge.
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0), // 0
                new RealPoint2D(2, 0), // 1
                new RealPoint2D(2, 2), // 2
                new RealPoint2D(0, 2), // 3
                new RealPoint2D(1, 0), // 4 midpoint on edge (0,1)
            };

            // Constrain from midpoint-on-edge to opposite vertex; exercises endpoint-on-edge handling.
            var segments = new List<(int A, int B)> { (4, 2) };
            var input = new Delaunay2DInput(points, segments);

            var result = Delaunay2DTriangulator.Run(in input);
            bool hasEdge = result.Triangles.Any(tri =>
                (tri.A == 4 && tri.B == 2) ||
                (tri.B == 4 && tri.C == 2) ||
                (tri.C == 4 && tri.A == 2) ||
                (tri.A == 2 && tri.B == 4) ||
                (tri.B == 2 && tri.C == 4) ||
                (tri.C == 2 && tri.A == 4));

            Assert.True(hasEdge, "Constraint edge (4,2) was not present in the triangulation.");
        }

        [Fact]
        public void Classifier_Distinguishes_CollinearOverlap_And_ProperInterior()
        {
            // Constraint segment AB: horizontal along y = 0 from x = 0 to x = 3.
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),   // 0 = A
                new RealPoint2D(3, 0),   // 1 = B
                new RealPoint2D(1, 0),   // 2 on AB line
                new RealPoint2D(2, 0),   // 3 on AB line
                new RealPoint2D(1, 1),   // 4 above line
                new RealPoint2D(1.5, -1) // 5 below line
            };

            var edge = new Edge2D(0, 1);

            // Triangle T_collinear: edge (2,3) is collinear with AB and overlaps [1,2] on y=0.
            // All interior is above the line (point 4), so AB never enters the interior.
            var tCollinear = new Triangle2D(2, 3, 4, points);

            // Triangle T_interior: vertices above and below y = 0 so that AB crosses the interior.
            // (4,1,5) gives a triangle that straddles the x-axis; AB intersects its interior.
            var tInterior = new Triangle2D(4, 1, 5, points);

            var kindCollinear = Geometry2DIntersections.ClassifySegmentTriangleIntersection(points, edge, tCollinear);
            var kindInterior = Geometry2DIntersections.ClassifySegmentTriangleIntersection(points, edge, tInterior);

            Assert.Equal(
                Geometry2DIntersections.SegmentTriangleIntersectionKind.CollinearOverlap,
                kindCollinear);

            Assert.Equal(
                Geometry2DIntersections.SegmentTriangleIntersectionKind.ProperInterior,
                kindInterior);
        }
    }
}
