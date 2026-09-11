using IO;
using Geometry;

namespace World;

public sealed partial class World
{
    public void Save(string path)
        => Save(path, 1.0);

    /// <summary>Exports with a positive coordinate scale, e.g. 0.01 for a grid unit of 0.01 mm.</summary>
    public void Save(string path, double coordinateScale)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));

        var triangles = new List<Triangle>();
        foreach (var shape in Shapes)
        {
            triangles.AddRange(shape.Mesh.Triangles);
        }

        StlWriter.Write(triangles, path, coordinateScale);
    }
}
