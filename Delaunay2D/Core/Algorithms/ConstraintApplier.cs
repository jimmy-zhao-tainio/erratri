using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    /// <summary>
    /// Wrapper over Delaunay2DTriangulator that retains mesh topology and constrained edges.
    /// </summary>
    internal sealed class ConstraintApplier
    {
        private MeshTopology2D _mesh = new ListBackedMeshTopology2D(Array.Empty<Triangle2D>());
        private IReadOnlyList<RealPoint2D> _points = Array.Empty<RealPoint2D>();
        private HashSet<Edge2D> _constrainedEdges = new HashSet<Edge2D>();

        internal MeshTopology2D Mesh => _mesh;

        internal Delaunay2DResult Run(in Delaunay2DInput input, bool validate = false)
        {
            var result = Delaunay2DTriangulator.Run(in input, validate);

            _points = input.Points;
            var tris = new List<Triangle2D>(result.Triangles.Count);
            foreach (var (a, b, c) in result.Triangles)
            {
                tris.Add(new Triangle2D(a, b, c, input.Points));
            }

            _mesh = new ListBackedMeshTopology2D(tris);
            _constrainedEdges = new HashSet<Edge2D>();
            foreach (var seg in input.Segments)
            {
                _constrainedEdges.Add(new Edge2D(seg.A, seg.B));
            }

            return result;
        }

        internal bool IsConstrained(Edge2D edge) => _constrainedEdges.Contains(edge);

        internal void Validate(bool checkConstraints)
        {
            MeshValidator2D.ValidateLocalMesh(_mesh.Triangles, _points, "ConstraintApplier.Validate");

            if (!checkConstraints)
            {
                return;
            }

            foreach (var edge in _constrainedEdges)
            {
                bool found = false;
                foreach (var tri in _mesh.Triangles)
                {
                    if (Geometry2DIntersections.TriangleHasUndirectedEdge(tri, edge.A, edge.B))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Constrained edge ({edge.A},{edge.B}) not present in mesh.");
                }
            }
        }
    }
}
