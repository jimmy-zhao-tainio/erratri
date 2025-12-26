using System.Collections.Generic;

namespace Geometry;

public readonly struct RealPolygon
{
    public IReadOnlyList<RealPoint> Vertices { get; }

    public RealPolygon(IReadOnlyList<RealPoint> vertices)
    {
        Vertices = vertices;
    }

    public double SignedArea
    {
        get
        {
            double area = 0.0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var a = Vertices[i];
                var b = Vertices[(i + 1) % Vertices.Count];
                area += a.X * b.Y - a.Y * b.X;
            }

            return 0.5 * area;
        }
    }
}
