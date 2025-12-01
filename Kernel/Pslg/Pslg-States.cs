using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Output of the Build phase: PSLG vertices and edges.
internal readonly struct PslgBuildState
{
    internal IReadOnlyList<PslgVertex> Vertices { get; }
    internal IReadOnlyList<PslgEdge> Edges { get; }

    internal PslgBuildState(IReadOnlyList<PslgVertex> vertices, IReadOnlyList<PslgEdge> edges)
    {
        Vertices = vertices;
        Edges = edges;
    }

    internal void Validate()
    {
#if DEBUG
        System.Diagnostics.Debug.Assert(Vertices != null, "Vertices should not be null.");
        System.Diagnostics.Debug.Assert(Edges != null, "Edges should not be null.");

        if (Vertices is null || Edges is null) return;

        int vCount = Vertices.Count;
        for (int i = 0; i < Edges.Count; i++)
        {
            var e = Edges[i];
            System.Diagnostics.Debug.Assert(e.Start >= 0 && e.Start < vCount, "Edge start index out of range.");
            System.Diagnostics.Debug.Assert(e.End >= 0 && e.End < vCount, "Edge end index out of range.");
        }

        // TODO: add a duplicate-vertex proximity check if needed (already enforced during normalization).
#endif
    }
}

// Output of the half-edge construction phase.
internal readonly struct PslgHalfEdgeState
{
    internal IReadOnlyList<PslgVertex> Vertices { get; }
    internal IReadOnlyList<PslgEdge> Edges { get; }
    internal IReadOnlyList<PslgHalfEdge> HalfEdges { get; }

    internal PslgHalfEdgeState(
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        IReadOnlyList<PslgHalfEdge> halfEdges)
    {
        Vertices = vertices;
        Edges = edges;
        HalfEdges = halfEdges;
    }

    internal void Validate()
    {
#if DEBUG
        System.Diagnostics.Debug.Assert(Vertices != null, "Vertices should not be null.");
        System.Diagnostics.Debug.Assert(Edges != null, "Edges should not be null.");
        System.Diagnostics.Debug.Assert(HalfEdges != null, "HalfEdges should not be null.");

        if (Vertices is null || HalfEdges is null) return;

        int vCount = Vertices.Count;
        int heCount = HalfEdges.Count;
        for (int i = 0; i < heCount; i++)
        {
            var he = HalfEdges[i];
            System.Diagnostics.Debug.Assert(he.From >= 0 && he.From < vCount, "HalfEdge.From out of range.");
            System.Diagnostics.Debug.Assert(he.To >= 0 && he.To < vCount, "HalfEdge.To out of range.");
            if (he.Twin >= 0)
            {
                System.Diagnostics.Debug.Assert(he.Twin < heCount, "HalfEdge.Twin out of range.");
            }
            if (he.Next >= 0)
            {
                System.Diagnostics.Debug.Assert(he.Next < heCount, "HalfEdge.Next out of range.");
            }
        }

        // TODO: cycle completeness/consistency checks could be added if needed.
#endif
    }
}

// Output of the face extraction phase.
internal readonly struct PslgFaceState
{
    internal IReadOnlyList<PslgVertex> Vertices { get; }
    internal IReadOnlyList<PslgEdge> Edges { get; }
    internal IReadOnlyList<PslgHalfEdge> HalfEdges { get; }
    internal IReadOnlyList<PslgFace> Faces { get; }

    internal PslgFaceState(
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        IReadOnlyList<PslgHalfEdge> halfEdges,
        IReadOnlyList<PslgFace> faces)
    {
        Vertices = vertices;
        Edges = edges;
        HalfEdges = halfEdges;
        Faces = faces;
    }

    internal void Validate()
    {
#if DEBUG
        System.Diagnostics.Debug.Assert(Vertices != null, "Vertices should not be null.");
        System.Diagnostics.Debug.Assert(Faces != null, "Faces should not be null.");

        if (Vertices is null || Faces is null) return;

        int vCount = Vertices.Count;
        for (int i = 0; i < Faces.Count; i++)
        {
            var face = Faces[i];
            System.Diagnostics.Debug.Assert(face.OuterVertices != null, "Face outer vertices should not be null.");
            if (face.OuterVertices is null) continue;

            System.Diagnostics.Debug.Assert(face.OuterVertices.Length >= 3, "Face outer ring should have at least 3 vertices.");
            foreach (var idx in face.OuterVertices)
            {
                System.Diagnostics.Debug.Assert(idx >= 0 && idx < vCount, "Face vertex index out of range.");
            }

            // TODO: consider asserting non-zero area if appropriate tolerance logic exists.
        }
#endif
    }
}

// Output of interior-face selection.
internal readonly struct PslgSelectionState
{
    internal IReadOnlyList<PslgVertex> Vertices { get; }
    internal IReadOnlyList<PslgEdge> Edges { get; }
    internal IReadOnlyList<PslgHalfEdge> HalfEdges { get; }
    internal IReadOnlyList<PslgFace> Faces { get; }
    internal PslgFaceSelection Selection { get; }

    internal PslgSelectionState(
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        IReadOnlyList<PslgHalfEdge> halfEdges,
        IReadOnlyList<PslgFace> faces,
        PslgFaceSelection selection)
    {
        Vertices = vertices;
        Edges = edges;
        HalfEdges = halfEdges;
        Faces = faces;
        Selection = selection;
    }

    internal void Validate()
    {
#if DEBUG
        System.Diagnostics.Debug.Assert(Selection.InteriorFaces != null, "Interior faces should not be null.");
        if (Selection.InteriorFaces is null || Faces is null) return;

        int faceCount = Faces.Count;
        foreach (var f in Selection.InteriorFaces)
        {
            // Faces are objects in the list; ensure they came from the face set
            // TODO: if we need to verify membership, add an identity check; for now, rely on selection construction.
            System.Diagnostics.Debug.Assert(f.OuterVertices != null && f.OuterVertices.Length >= 3, "Interior face should have an outer ring.");
        }

        // TODO: area consistency checks could be added here if needed.
#endif
    }
}

// Output of the triangulation phase.
internal readonly struct PslgTriangulationState
{
    internal IReadOnlyList<PslgVertex> Vertices { get; }
    internal IReadOnlyList<PslgEdge> Edges { get; }
    internal IReadOnlyList<PslgHalfEdge> HalfEdges { get; }
    internal IReadOnlyList<PslgFace> Faces { get; }
    internal PslgFaceSelection Selection { get; }
    internal IReadOnlyList<RealTriangle> Patches { get; }

    internal PslgTriangulationState(
        IReadOnlyList<PslgVertex> vertices,
        IReadOnlyList<PslgEdge> edges,
        IReadOnlyList<PslgHalfEdge> halfEdges,
        IReadOnlyList<PslgFace> faces,
        PslgFaceSelection selection,
        IReadOnlyList<RealTriangle> patches)
    {
        Vertices = vertices;
        Edges = edges;
        HalfEdges = halfEdges;
        Faces = faces;
        Selection = selection;
        Patches = patches;
    }

    internal void Validate()
    {
#if DEBUG
        System.Diagnostics.Debug.Assert(Patches != null, "Patches should not be null.");
        if (Patches is null) return;

        // Optional: ensure positive area for patches.
        // TODO: add area checks if desired (requires RealTriangle access in debug assertions).
#endif
    }
}
