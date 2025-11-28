using Geometry; // for Triangle
using Topology; // for Mesh

namespace World;

public abstract partial class Shape
{
    // Every shape exposes a closed surface mesh directly.
    public Mesh Mesh { get; protected set; } = new Mesh(Array.Empty<Triangle>());
}
