using System;
using System.Collections.Generic;
using Geometry;

namespace Delaunay2D
{
    internal interface MeshTopology2D
    {
        IReadOnlyList<Triangle2D> Triangles { get; }
        IEnumerable<Edge2D> Edges { get; }
        IEnumerable<int> IncidentTriangles(Edge2D edge);
        IEnumerable<int> AdjacentTriangles(int triangleIndex);
        bool HasEdge(Edge2D edge);
    }

    internal sealed class ListBackedMeshTopology2D : MeshTopology2D
    {
        private readonly Dictionary<Edge2D, List<int>> _edgeToTriangles;
        public IReadOnlyList<Triangle2D> Triangles { get; }

        public ListBackedMeshTopology2D(IReadOnlyList<Triangle2D> triangles)
        {
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            _edgeToTriangles = new Dictionary<Edge2D, List<int>>();
            BuildIndex();
        }

        public IEnumerable<Edge2D> Edges => _edgeToTriangles.Keys;

        public IEnumerable<int> IncidentTriangles(Edge2D edge)
        {
            if (_edgeToTriangles.TryGetValue(edge, out var list))
            {
                foreach (var idx in list)
                {
                    yield return idx;
                }
            }
        }

        public IEnumerable<int> AdjacentTriangles(int triangleIndex)
        {
            var tri = Triangles[triangleIndex];
            var edges = new Edge2D[]
            {
                new Edge2D(tri.A, tri.B),
                new Edge2D(tri.B, tri.C),
                new Edge2D(tri.C, tri.A)
            };

            var seen = new HashSet<int>();
            foreach (var e in edges)
            {
                if (_edgeToTriangles.TryGetValue(e, out var list))
                {
                    foreach (var idx in list)
                    {
                        if (idx != triangleIndex && seen.Add(idx))
                        {
                            yield return idx;
                        }
                    }
                }
            }
        }

        public bool HasEdge(Edge2D edge) => _edgeToTriangles.ContainsKey(edge);

        private void BuildIndex()
        {
            for (int i = 0; i < Triangles.Count; i++)
            {
                var tri = Triangles[i];
                AddEdge(new Edge2D(tri.A, tri.B), i);
                AddEdge(new Edge2D(tri.B, tri.C), i);
                AddEdge(new Edge2D(tri.C, tri.A), i);
            }
        }

        private void AddEdge(Edge2D edge, int triangleIndex)
        {
            if (_edgeToTriangles.TryGetValue(edge, out var list))
            {
                list.Add(triangleIndex);
            }
            else
            {
                _edgeToTriangles[edge] = new List<int> { triangleIndex };
            }
        }
    }
}
