using System;

namespace Delaunay2D
{
    /// <summary>
    /// Undirected edge between two distinct vertex indices.
    /// Stored in normalized (min,max) order so (a,b) == (b,a) for equality and hashing.
    /// </summary>
    public readonly struct Edge2D : IEquatable<Edge2D>
    {
        public int A { get; }
        public int B { get; }

        public Edge2D(int a, int b)
        {
            if (a == b)
            {
                throw new ArgumentException("Edge2D requires two distinct vertex indices.", nameof(a));
            }

            A = Math.Min(a, b);
            B = Math.Max(a, b);
        }

        public bool Equals(Edge2D other) => A == other.A && B == other.B;

        public override bool Equals(object? obj) => obj is Edge2D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(A, B);
    }
}
