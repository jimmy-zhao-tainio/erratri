using System;
using System.Collections.Generic;

namespace Kernel;

internal sealed class PslgFace
{
    // CCW outer boundary vertex indices.
    public int[] OuterVertices { get; }

    // Zero or more CCW interior cycles (nested boundaries).
    public IReadOnlyList<int[]> InteriorCycles { get; }

    // Signed area in UV: outer ring area minus sum(interior cycles).
    public double SignedAreaUV { get; }

    // Backward compatibility helpers for older tests/usage.
    public int[] VertexIndices => OuterVertices;
    public double SignedArea => SignedAreaUV;

    public PslgFace(int[] outerVertices, double signedArea)
        : this(outerVertices, Array.Empty<int[]>(), signedArea)
    {
    }

    public PslgFace(int[] outerVertices, IReadOnlyList<int[]> interiorCycles, double signedArea)
    {
        OuterVertices = outerVertices;
        InteriorCycles = interiorCycles;
        SignedAreaUV = signedArea;
    }
}
