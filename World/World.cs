namespace World;

public sealed partial class World
{
    private readonly List<Shape> shapes = new();

    public IReadOnlyList<Shape> Shapes => shapes;

    public void Add(Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        shapes.Add(shape);
    }
}
