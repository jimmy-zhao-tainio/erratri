using Geometry;

namespace Boolean;

public readonly struct TrianglePatch
{
    public RealTriangle Triangle { get; }
    public int FaceId { get; }
    public TriangleVertexIds VertexIds { get; }
    public CoplanarOwner CoplanarOwner { get; }

    public TrianglePatch(
        RealTriangle triangle,
        int faceId,
        TriangleVertexIds vertexIds,
        CoplanarOwner coplanarOwner)
    {
        Triangle = triangle;
        FaceId = faceId;
        VertexIds = vertexIds;
        CoplanarOwner = coplanarOwner;
    }
}
