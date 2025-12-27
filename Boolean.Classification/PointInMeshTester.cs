using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates;
using Geometry.Topology;

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

    public Containment Classify(in RealPoint point)
    {
        if (triangles.Count == 0 || bounds.IsEmpty)
        {
            return Containment.Outside;
        }

        double epsDist = Tolerances.PlaneSideEpsilon;
        double epsBary = Tolerances.BarycentricInsideEpsilon;

        var min = new RealPoint(point.X - epsDist, point.Y - epsDist, point.Z - epsDist);
        var max = new RealPoint(point.X + epsDist, point.Y + epsDist, point.Z + epsDist);
        var onBox = BoundingBox.FromPoints(in min, in max);
        var onCandidates = new List<int>();
        tree.Query(onBox, onCandidates);

        for (int i = 0; i < onCandidates.Count; i++)
        {
            var tri = new RealTriangle(triangles[onCandidates[i]]);
            if (RealTrianglePredicates.IsOnTriangle(tri, point, epsDist, epsBary))
            {
                return Containment.On;
            }
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

        return (crossings & 1) == 1 ? Containment.Inside : Containment.Outside;
    }

    public bool Contains(in RealPoint point)
        => Classify(in point) == Containment.Inside;
}

