using System;
using System.Collections.Generic;
using System.Linq;
using Delaunay2D;
using Geometry;
using Xunit;

namespace Delaunay2D.Tests
{
    public class ConstraintApplierTests
    {
        [Fact]
        public void MeshTopology_ExposesEdgesAndAdjacency()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(0, 1)
            };

            var applier = new ConstraintApplier();
            var input = new Delaunay2DInput(points, Array.Empty<(int A, int B)>());
            applier.Run(in input);

            var mesh = applier.Mesh;
            var edges = mesh.Edges.ToList();
            Assert.Equal(3, edges.Count);
            Assert.True(mesh.HasEdge(new Edge2D(0, 1)));
            Assert.True(mesh.HasEdge(new Edge2D(1, 2)));
            Assert.True(mesh.HasEdge(new Edge2D(2, 0)));

            var adj = mesh.AdjacentTriangles(0).ToList();
            Assert.Empty(adj);

            var incident = mesh.IncidentTriangles(new Edge2D(0, 1)).ToList();
            Assert.Single(incident);
            Assert.Equal(0, incident[0]);
        }

        [Fact]
        public void ConstraintEdges_AreTracked()
        {
            var points = new List<RealPoint2D>
            {
                new RealPoint2D(0, 0),
                new RealPoint2D(1, 0),
                new RealPoint2D(1, 1),
                new RealPoint2D(0, 1)
            };

            var segments = new List<(int A, int B)> { (0, 2) };
            var applier = new ConstraintApplier();
            var input = new Delaunay2DInput(points, segments);
            applier.Run(in input);

            Assert.True(applier.IsConstrained(new Edge2D(0, 2)));
            applier.Validate(checkConstraints: true);
        }
    }
}
