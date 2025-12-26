using System;
using System.Collections.Generic;
using Geometry;

namespace Geometry.Topology;

// Final boolean mesh result: merged vertices and triangles.
public sealed class RealMesh
{
    public IReadOnlyList<RealPoint> Vertices { get; }
    public IReadOnlyList<(int A, int B, int C)> Triangles { get; }

    public RealMesh(
        IReadOnlyList<RealPoint> vertices,
        IReadOnlyList<(int A, int B, int C)> triangles)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
    }
}

