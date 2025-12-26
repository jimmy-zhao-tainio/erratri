using System;
using Boolean.Pipeline;
using Geometry.Topology;

namespace Boolean;

// Simple facade around the existing boolean mesher for Mesh inputs.
public static class Operation
{
    public static RealMesh Union(Mesh a, Mesh b) =>
        Run(BooleanOperationType.Union, a, b);

    public static RealMesh Intersection(Mesh a, Mesh b) =>
        Run(BooleanOperationType.Intersection, a, b);

    public static RealMesh DifferenceAB(Mesh a, Mesh b) =>
        Run(BooleanOperationType.DifferenceAB, a, b);

    public static RealMesh DifferenceBA(Mesh a, Mesh b) =>
        Run(BooleanOperationType.DifferenceBA, a, b);

    public static RealMesh SymmetricDifference(Mesh a, Mesh b) =>
        Run(BooleanOperationType.SymmetricDifference, a, b);

    private static RealMesh Run(BooleanOperationType op, Mesh a, Mesh b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        return PipelineEntry.Run(a, b, op);
    }
}

