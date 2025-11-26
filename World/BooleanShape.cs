using Topology;

namespace World;

public class BooleanShape : Shape
{
    private readonly Shape left;
    private readonly Shape right;
    private readonly Kernel.BooleanOperation op;

    public BooleanShape(Shape left, Shape right, Kernel.BooleanOperation op)
    {
        this.left = left ?? throw new ArgumentNullException(nameof(left));
        this.right = right ?? throw new ArgumentNullException(nameof(right));
        this.op = op;
        Mesh = BuildMesh();
    }

    private ClosedSurface BuildMesh()
    {
        var result = op switch
        {
            Kernel.BooleanOperation.Union => Kernel.BooleanOps.Union(left.Mesh, right.Mesh),
            Kernel.BooleanOperation.Intersection => Kernel.BooleanOps.Intersection(left.Mesh, right.Mesh),
            Kernel.BooleanOperation.DifferenceAB => Kernel.BooleanOps.DifferenceAB(left.Mesh, right.Mesh),
            Kernel.BooleanOperation.DifferenceBA => Kernel.BooleanOps.DifferenceBA(left.Mesh, right.Mesh),
            Kernel.BooleanOperation.SymmetricDifference => Kernel.BooleanOps.SymmetricDifference(left.Mesh, right.Mesh),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation")
        };

        return Kernel.BooleanMeshConverter.ToClosedSurface(result);
    }
}

public sealed class Union : BooleanShape
{
    public Union(Shape left, Shape right)
        : base(left, right, Kernel.BooleanOperation.Union) { }
}

public sealed class Intersection : BooleanShape
{
    public Intersection(Shape left, Shape right)
        : base(left, right, Kernel.BooleanOperation.Intersection) { }
}

public sealed class DifferenceAB : BooleanShape
{
    public DifferenceAB(Shape left, Shape right)
        : base(left, right, Kernel.BooleanOperation.DifferenceAB) { }
}

public sealed class DifferenceBA : BooleanShape
{
    public DifferenceBA(Shape left, Shape right)
        : base(left, right, Kernel.BooleanOperation.DifferenceBA) { }
}

public sealed class SymmetricDifference : BooleanShape
{
    public SymmetricDifference(Shape left, Shape right)
        : base(left, right, Kernel.BooleanOperation.SymmetricDifference) { }
}
