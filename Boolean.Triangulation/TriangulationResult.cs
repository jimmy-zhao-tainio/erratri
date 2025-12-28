using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

public readonly struct TriangulationResult
{
    public IReadOnlyList<RealTriangle> Triangles { get; }
    public IReadOnlyList<int> FaceIds { get; }

    public TriangulationResult(IReadOnlyList<RealTriangle> triangles, IReadOnlyList<int> faceIds)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        if (faceIds is null) throw new ArgumentNullException(nameof(faceIds));
        if (triangles.Count != faceIds.Count)
        {
            throw new ArgumentException("Triangle and face-id counts must match.");
        }

        Triangles = triangles;
        FaceIds = faceIds;
    }
}
