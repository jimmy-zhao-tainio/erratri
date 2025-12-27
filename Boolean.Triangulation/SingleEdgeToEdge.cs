using System;
using System.Collections.Generic;
using Geometry;

namespace Boolean;

internal static class SingleEdgeToEdge
{
    /// <summary>
    /// Fast-path triangulation for a single interior chord whose endpoints lie
    /// on two distinct adjacent triangle edges.
    ///
    /// Preconditions:
    ///   - Exactly one segment is provided.
    ///   - Both endpoints lie on triangle edges (not in the interior).
    ///   - Endpoints lie on two distinct edges that are adjacent in the
    ///     oriented edge cycle 0-&gt;1-&gt;2-&gt;0.
    ///
    /// This method does NOT handle:
    ///   - Interior endpoints,
    ///   - Endpoints on the same edge,
    ///   - Endpoints at (or very close to) triangle vertices,
    ///   - Multiple segments or junctions.
    ///
    /// Unsupported configurations must be handled by the generic PSLG-based
    /// subdivision path, not widened here. For valid inputs this returns
    /// exactly three patches whose union reconstructs the original triangle
    /// and for which the chord appears as patch edges.
    /// </summary>
    internal static IReadOnlyList<RealTriangle> Triangulate(
        in Triangle triangle,
        IReadOnlyList<IntersectionPoint> points,
        IReadOnlyList<IntersectionSegment> segments)
    {
        if (segments.Count != 1)
        {
            throw new ArgumentException("SingleEdgeToEdge subdivision requires exactly one segment.", nameof(segments));
        }

        var seg = segments[0];
        if (seg.StartIndex < 0 || seg.StartIndex >= points.Count ||
            seg.EndIndex < 0 || seg.EndIndex >= points.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(segments), "Segment indices must be valid point indices.");
        }

        var pA = points[seg.StartIndex];
        var pB = points[seg.EndIndex];

        // Reject endpoints that are (numerically) at triangle vertices. This
        // fast path is intended for interior chords; vertex endpoints should
        // be handled by the general PSLG subdivision instead.
        if (Triangulation.IsAtVertex(pA.Barycentric) ||
            Triangulation.IsAtVertex(pB.Barycentric))
        {
            throw new InvalidOperationException(
                "SubdivideSingleEdgeToEdge does not support vertex endpoints; use the PSLG path.");
        }

        var locA = Triangulation.ClassifyEdge(pA.Barycentric);
        var locB = Triangulation.ClassifyEdge(pB.Barycentric);

        if (locA == EdgeLocation.Interior || locB == EdgeLocation.Interior || locA == locB)
        {
            throw new InvalidOperationException(
                "SubdivideSingleEdgeToEdge requires both endpoints to lie on distinct triangle edges.");
        }

        // Map EdgeLocation to integer edge index 0,1,2.
        static int EdgeIndex(EdgeLocation loc) => loc switch
        {
            EdgeLocation.Edge0 => 0,
            EdgeLocation.Edge1 => 1,
            EdgeLocation.Edge2 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(loc), "Unsupported edge location.")
        };

        int edgeA = EdgeIndex(locA);
        int edgeB = EdgeIndex(locB);

        var baryP = pA.Barycentric;
        var baryQ = pB.Barycentric;

        // Canonicalize order so that edgeQ is the next edge after edgeP
        // in the oriented cycle 0->1->2->0. If not, swap endpoints.
        if (edgeB != ((edgeA + 1) % 3))
        {
            if (edgeA == ((edgeB + 1) % 3))
            {
                (edgeA, edgeB) = (edgeB, edgeA);
                (baryP, baryQ) = (baryQ, baryP);
            }
            else
            {
                throw new InvalidOperationException(
                    "SubdivideSingleEdgeToEdge expects endpoints on two edges that are adjacent in the edge cycle.");
            }
        }

        var v0 = new RealPoint(triangle.P0);
        var v1 = new RealPoint(triangle.P1);
        var v2 = new RealPoint(triangle.P2);

        var rp = Barycentric.ToRealPointOnTriangle(in triangle, in baryP);
        var rq = Barycentric.ToRealPointOnTriangle(in triangle, in baryQ);

        var patches = new List<RealTriangle>(3);

        if (edgeA == 0 && edgeB == 1)
        {
            // P on edge V0-V1, Q on edge V1-V2.
            // Triangle: rp, v1, rq
            // Quadrilateral: rq, v2, v0, rp triangulated as:
            //   rq, v2, v0 and rq, v0, rp
            patches.Add(new RealTriangle(rp, v1, rq));
            patches.Add(new RealTriangle(rq, v2, v0));
            patches.Add(new RealTriangle(rq, v0, rp));
        }
        else if (edgeA == 1 && edgeB == 2)
        {
            // P on edge V1-V2, Q on edge V2-V0.
            // Triangle: rp, v2, rq
            // Quadrilateral: rq, v0, v1, rp triangulated as:
            //   rq, v0, v1 and rq, v1, rp
            patches.Add(new RealTriangle(rp, v2, rq));
            patches.Add(new RealTriangle(rq, v0, v1));
            patches.Add(new RealTriangle(rq, v1, rp));
        }
        else if (edgeA == 2 && edgeB == 0)
        {
            // P on edge V2-V0, Q on edge V0-V1.
            // Triangle: rp, v0, rq
            // Quadrilateral: rq, v1, v2, rp triangulated as:
            //   rq, v1, v2 and rq, v2, rp
            patches.Add(new RealTriangle(rp, v0, rq));
            patches.Add(new RealTriangle(rq, v1, v2));
            patches.Add(new RealTriangle(rq, v2, rp));
        }
        else
        {
            throw new InvalidOperationException(
                "SubdivideSingleEdgeToEdge reached an unsupported edge pair after canonicalization.");
        }

        return patches;
    }
}
