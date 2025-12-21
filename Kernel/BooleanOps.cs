using System;
using Topology;

namespace Kernel;

// Simple facade around the existing boolean mesher for Mesh inputs.
public static class BooleanOps
{
    public static RealMesh Union(Mesh a, Mesh b) =>
        Run(BooleanOperation.Union, a, b);

    public static RealMesh Intersection(Mesh a, Mesh b) =>
        Run(BooleanOperation.Intersection, a, b);

    public static RealMesh DifferenceAB(Mesh a, Mesh b) =>
        Run(BooleanOperation.DifferenceAB, a, b);

    public static RealMesh DifferenceBA(Mesh a, Mesh b) =>
        Run(BooleanOperation.DifferenceBA, a, b);

    public static RealMesh SymmetricDifference(Mesh a, Mesh b) =>
        Run(BooleanOperation.SymmetricDifference, a, b);

    private static RealMesh Run(BooleanOperation op, Mesh a, Mesh b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        return AssemblyEntry.Run(a, b, op);
    }
}
