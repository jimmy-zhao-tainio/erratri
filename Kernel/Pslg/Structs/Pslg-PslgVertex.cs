using System;

namespace Kernel;

internal readonly struct PslgVertex
{
    public double X { get; }
    public double Y { get; }

    // True if this vertex is one of the three triangle corners.
    public bool IsTriangleCorner { get; }

    // 0,1,2 for triangle corners; -1 otherwise.
    public int CornerIndex { get; }

    public PslgVertex(double x, double y, bool isTriangleCorner, int cornerIndex)
    {
        X = x;
        Y = y;
        IsTriangleCorner = isTriangleCorner;
        CornerIndex = cornerIndex;
    }
}
