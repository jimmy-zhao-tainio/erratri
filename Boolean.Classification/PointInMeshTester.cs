using System;
using System.Collections.Generic;
using Geometry;
using Topology;

namespace Boolean;

internal sealed class PointInMeshTester
{
    private readonly IReadOnlyList<Triangle> triangles;
    private readonly BoundingBoxTree tree;
    private readonly BoundingBox bounds;
    private readonly RealNormal rayDirection;
    private readonly double maxRayLength;

    public PointInMeshTester(IReadOnlyList<Triangle> triangles)
    {
        this.triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
        tree = new BoundingBoxTree(triangles);
        bounds = BoundingBox.FromTriangles(triangles);

        var dirVector = new RealVector(1.0, 0.3141592653589793, 0.2718281828459045);
        rayDirection = RealNormal.FromVector(dirVector);
        maxRayLength = bounds.MaximumRayLength;
    }

    public bool Contains(in RealPoint point)
    {
        if (triangles.Count == 0 || bounds.IsEmpty)
        {
            return false;
        }

        var end = new RealPoint(
            point.X + rayDirection.X * maxRayLength,
            point.Y + rayDirection.Y * maxRayLength,
            point.Z + rayDirection.Z * maxRayLength);

        var rayBox = BoundingBox.FromPoints(in point, in end);
        var candidates = new List<int>();
        tree.Query(rayBox, candidates);

        int crossings = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            int idx = candidates[i];
            if (RayIntersectsTriangleHelper.RayIntersectsTriangle(point, rayDirection, triangles[idx], maxRayLength))
            {
                crossings++;
            }
        }

        return (crossings & 1) == 1;
    }
}
