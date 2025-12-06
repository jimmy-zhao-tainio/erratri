using System;

namespace Delaunay2D
{
    internal readonly struct Edge2D : IEquatable<Edge2D>
    {
        public int A { get; }
        public int B { get; }

        public Edge2D(int a, int b)
        {
            if (a == b)
            {
                throw new ArgumentException("Edge endpoints must be distinct.", nameof(a));
            }

            if (a < b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public bool Equals(Edge2D other) => A == other.A && B == other.B;

        public override bool Equals(object? obj) => obj is Edge2D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(A, B);
    }
}
