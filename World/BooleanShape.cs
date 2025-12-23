using Geometry.Topology;

namespace World;

public class BooleanShape : Shape
{
    private readonly Shape left;
    private readonly Shape right;
    private readonly Boolean.BooleanOperationType op;

    public BooleanShape(Shape left, Shape right, Boolean.BooleanOperationType op)
    {
        this.left = left ?? throw new ArgumentNullException(nameof(left));
        this.right = right ?? throw new ArgumentNullException(nameof(right));
        this.op = op;
        Mesh = BuildMesh();
    }

    private Mesh BuildMesh()
    {
        var result = op switch
        {
            Boolean.BooleanOperationType.Union => Boolean.Operation.Union(left.Mesh, right.Mesh),
            Boolean.BooleanOperationType.Intersection => Boolean.Operation.Intersection(left.Mesh, right.Mesh),
            Boolean.BooleanOperationType.DifferenceAB => Boolean.Operation.DifferenceAB(left.Mesh, right.Mesh),
            Boolean.BooleanOperationType.DifferenceBA => Boolean.Operation.DifferenceBA(left.Mesh, right.Mesh),
            Boolean.BooleanOperationType.SymmetricDifference => Boolean.Operation.SymmetricDifference(left.Mesh, right.Mesh),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported boolean operation")
        };

        return Boolean.BooleanMeshConverter.ToMesh(result);
    }
}

public sealed class Union : BooleanShape
{
    public Union(Shape left, Shape right)
        : base(left, right, Boolean.BooleanOperationType.Union) { }
}

public sealed class Intersection : BooleanShape
{
    public Intersection(Shape left, Shape right)
        : base(left, right, Boolean.BooleanOperationType.Intersection) { }
}

public sealed class DifferenceAB : BooleanShape
{
    public DifferenceAB(Shape left, Shape right)
        : base(left, right, Boolean.BooleanOperationType.DifferenceAB) { }
}

public sealed class DifferenceBA : BooleanShape
{
    public DifferenceBA(Shape left, Shape right)
        : base(left, right, Boolean.BooleanOperationType.DifferenceBA) { }
}

public sealed class SymmetricDifference : BooleanShape
{
    public SymmetricDifference(Shape left, Shape right)
        : base(left, right, Boolean.BooleanOperationType.SymmetricDifference) { }
}

