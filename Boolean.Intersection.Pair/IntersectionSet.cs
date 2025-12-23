using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Topology;

namespace Boolean;

// Represents two triangle meshes plus the sparse set of intersecting
// triangle pairs between them, classified by IntersectionType.
public readonly struct IntersectionSet
{
    public readonly struct Intersection
    {
        public int TriangleIndexA { get; }
        public int TriangleIndexB { get; }
        public IntersectionType Type { get; }

        public Intersection(int triangleIndexA, int triangleIndexB, IntersectionType type)
        {
            TriangleIndexA = triangleIndexA;
            TriangleIndexB = triangleIndexB;
            Type = type;
        }
    }

    public IReadOnlyList<Triangle> TrianglesA { get; }
    public IReadOnlyList<Triangle> TrianglesB { get; }
    public IReadOnlyList<Intersection> Intersections { get; }

    public IntersectionSet(IReadOnlyList<Triangle> trianglesA,
                           IReadOnlyList<Triangle> trianglesB)
    {
        if (trianglesA is null) throw new ArgumentNullException(nameof(trianglesA));
        if (trianglesB is null) throw new ArgumentNullException(nameof(trianglesB));

        TrianglesA = trianglesA;
        TrianglesB = trianglesB;

        var intersections = new List<Intersection>();

        if (trianglesA.Count == 0 || trianglesB.Count == 0)
        {
            Intersections = intersections;
            return;
        }

        var tree = new BoundingBoxTree(trianglesB);
        var candidates = new List<int>();

        for (int i = 0; i < trianglesA.Count; i++)
        {
            var triangleA = trianglesA[i];
            var boxA = BoundingBox.FromPoints(in triangleA.P0, in triangleA.P1, in triangleA.P2);

            candidates.Clear();
            tree.Query(in boxA, candidates);

            for (int j = 0; j < candidates.Count; j++)
            {
                int indexB = candidates[j];
                var triangleB = trianglesB[indexB];

                var type = IntersectionTypes.Classify(in triangleA, in triangleB);
                if (type == IntersectionType.None)
                    continue;

                intersections.Add(new Intersection(i, indexB, type));
            }
        }

        Intersections = intersections;
    }

    public (Triangle TriangleA, Triangle TriangleB, IntersectionType Type) Resolve(int index)
    {
        var ti = Intersections[index];
        var triangleA = TrianglesA[ti.TriangleIndexA];
        var triangleB = TrianglesB[ti.TriangleIndexB];
        return (triangleA, triangleB, ti.Type);
    }
}

public static class Kernel
{
    public static IntersectionSet Intersections(IReadOnlyList<Triangle> trianglesA, IReadOnlyList<Triangle> trianglesB)
        => new IntersectionSet(trianglesA, trianglesB);
}

