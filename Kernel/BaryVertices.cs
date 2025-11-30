using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Predicates.Internal;

namespace Kernel;

// Small helper for "candidate intersection points for one pair".
//
// For a single triangle pair we often have several candidate points
// that might form a segment or polygon. This class:
//
//   - Stores the world-space point together with its local PairVertex index
//   - Can answer "which two vertices are farthest apart?"
//
// The actual PairVertex list lives in PairFeatures; this type just
// helps decide *which* vertex indices to connect with a segment.
internal sealed class BaryVertices
{
    private readonly List<PairVertexSample3D> _samples = new();

    public int Count => _samples.Count;

    public IReadOnlyList<PairVertexSample3D> Samples => _samples;

    // Remember that "vertexIndex" in the PairFeatures.Vertices list
    // has a 3D position "point" in world space.
    public void Add(int vertexIndex, in RealPoint point)
    {
        _samples.Add(new PairVertexSample3D(vertexIndex, point));
    }

    // Brute-force farthest pair in this small set.
    //
    // We only ever have a handful of intersection samples per pair, so an
    // O(n^2) search is perfectly fine here. The result is reported as
    // vertex indices (not positions), so callers can reuse the existing
    // PairVertex array.
    public void FindFarthestPair(out int startVertexIndex, out int endVertexIndex)
    {
        PairVertexSample3D.FindFarthestPair(_samples, out startVertexIndex, out endVertexIndex);
    }
}

internal readonly struct PairVertexSample3D
{
    public int VertexIndex { get; }
    public RealPoint Point { get; }

    public PairVertexSample3D(int vertexIndex, RealPoint point)
    {
        VertexIndex = vertexIndex;
        Point = point;
    }

    // Find the pair of samples whose 3D points are farthest apart
    // in squared-distance sense. Returns the corresponding vertex
    // indices from the PairVertex list.
    public static void FindFarthestPair(IReadOnlyList<PairVertexSample3D> samples,
                                        out int startVertexIndex,
                                        out int endVertexIndex)
    {
        if (samples is null) throw new ArgumentNullException(nameof(samples));
        if (samples.Count == 0) throw new ArgumentException("Samples must be non-empty.", nameof(samples));

        startVertexIndex = samples[0].VertexIndex;
        endVertexIndex = samples[0].VertexIndex;
        double maxSqDist = 0.0;

        for (int i = 0; i < samples.Count - 1; i++)
        {
            var pi = samples[i].Point;
            for (int j = i + 1; j < samples.Count; j++)
            {
                var pj = samples[j].Point;
                double d2 = pj.DistanceSquared(pi);
                if (d2 > maxSqDist)
                {
                    maxSqDist = d2;
                    startVertexIndex = samples[i].VertexIndex;
                    endVertexIndex = samples[j].VertexIndex;
                }
            }
        }
    }
}

// Same idea as BaryVertices, but for the 2D projected coplanar case.
//
// Each entry is:
//   - a PairVertex index, and
//   - its 2D position in the projection plane.
//
// This is used to:
//   - find the farthest-apart vertices in 2D (for "segment" cases)
//   - build a simple ordered polygon loop from the unique vertices
//     (for "area" overlap cases).
internal sealed class BaryVertices2D
{
    private readonly List<PairVertexSample2D> _samples = new();
    public int Count => _samples.Count;
    public IReadOnlyList<PairVertexSample2D> Samples => _samples;

    public void Add(int vertexIndex, in RealPoint2D point)
    {
        _samples.Add(new PairVertexSample2D(vertexIndex, point));
    }

    // 2D version of "farthest pair" for coplanar projected points.
    public void FindFarthestPair(out int startVertexIndex, out int endVertexIndex)
    {
        PairVertexSample2D.FindFarthestPair(_samples, out startVertexIndex, out endVertexIndex);
    }

    // Build a simple polygon loop from the samples we have.
    //
    // Steps:
    //   1. Remove duplicate vertex indices while keeping their first 2D point.
    //   2. Compute the centroid of all remaining points.
    //   3. Sort the vertices by angle around the centroid.
    //
    // Because the overlap of two triangles is always convex, this cheap
    // "angle sort" is enough to give us a sane vertex order for the area
    // intersection on this pair.
    public List<int> BuildOrderedUniqueLoop()
    {
        if (_samples.Count == 0)
            return new List<int>();

        // Compute centroid of all sample points.
        double sumX = 0.0;
        double sumY = 0.0;
        for (int i = 0; i < _samples.Count; i++)
        {
            var p = _samples[i].Point;
            sumX += p.X;
            sumY += p.Y;
        }

        double invCount = 1.0 / _samples.Count;
        var centroid = new RealPoint2D(sumX * invCount, sumY * invCount);

        // Sort samples around the centroid by polar angle.
        _samples.Sort((a, b) =>
        {
            var daX = a.Point.X - centroid.X;
            var daY = a.Point.Y - centroid.Y;
            var dbX = b.Point.X - centroid.X;
            var dbY = b.Point.Y - centroid.Y;

            var angleA = Math.Atan2(daY, daX);
            var angleB = Math.Atan2(dbY, dbX);
            return angleA.CompareTo(angleB);
        });

        // Collapse multiple occurrences of the same VertexIndex so that
        // each logical vertex appears at most once in the loop.
        var orderedUnique = new List<int>(_samples.Count);
        var seen = new HashSet<int>();
        for (int i = 0; i < _samples.Count; i++)
        {
            int vertexIndex = _samples[i].VertexIndex;
            if (seen.Add(vertexIndex))
                orderedUnique.Add(vertexIndex);
        }
        return orderedUnique;
    }
}

internal readonly struct PairVertexSample2D
{
    public int VertexIndex { get; }
    public RealPoint2D Point { get; }

    public PairVertexSample2D(int vertexIndex, RealPoint2D point)
    {
        VertexIndex = vertexIndex;
        Point = point;
    }

    public static void FindFarthestPair(IReadOnlyList<PairVertexSample2D> samples,
                                        out int startVertexIndex,
                                        out int endVertexIndex)
    {
        if (samples is null) throw new ArgumentNullException(nameof(samples));
        if (samples.Count == 0) throw new ArgumentException("Samples must be non-empty.", nameof(samples));

        startVertexIndex = samples[0].VertexIndex;
        endVertexIndex = samples[0].VertexIndex;
        double maxSqDist = 0.0;

        for (int i = 0; i < samples.Count - 1; i++)
        {
            var pi = samples[i].Point;
            for (int j = i + 1; j < samples.Count; j++)
            {
                var pj = samples[j].Point;
                double d2 = new RealPoint(pj.X, pj.Y, 0.0).DistanceSquared(new RealPoint(pi.X, pi.Y, 0.0));
                if (d2 > maxSqDist)
                {
                    maxSqDist = d2;
                    startVertexIndex = samples[i].VertexIndex;
                    endVertexIndex = samples[j].VertexIndex;
                }
            }
        }
    }
}
