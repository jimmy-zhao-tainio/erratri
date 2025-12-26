namespace Geometry;

/*
  IMPORTANT: Tetrahedron is a locked-down, immutable value-object.

  Design contract (do not relax):
  - Immutable: A, B, C, D and all triangle data are readonly; no mutators.
  - Grid space only: no world scaling, no UnitScale, no external vertex lists.
  - Canonical orientation: constructor guarantees positive 6Ã— volume (throws if degenerate).
  - Triangle normals: precomputed, outward, unit-length; computed via Int128 cross/dot for robustness.
  - Coordinate bounds: each coordinate must be within Â±(2^62 âˆ’ 1) to ensure Int128 safety.
  - Separation of types: Normals are Normal (not a general vector). Do not add conversions from Normal â†’ vector.

  LLM/AI MAINTAINERS â€” DO NOT MODIFY THIS TYPE:
  - Do not add new public members, fields, or behavior here.
  - Do not change numeric strategy (Int128 for orientation; double normalization).
  - If you need helpers, create a separate utility (e.g., TetrahedronOps) and add tests there.
  - Any functional change must be accompanied by updated tests proving invariants still hold.
*/

// Immutable tetrahedron defined directly by four grid points (A, B, C, D).
// The constructor canonicalizes orientation so that the signed 6Ã— volume is positive.
// Exposes the real volume (divided by 6) as a positive readonly property.
// Exposes the four triangles (ABC, ABD, ACD, BCD) with precomputed outward unit normals.
public readonly struct Tetrahedron
{
    public readonly Point A;
    public readonly Point B;
    public readonly Point C;
    public readonly Point D;

    // Real (geometric) volume in grid units^3 (positive).
    public readonly double Volume;

    public readonly Triangle ABC;
    public readonly Triangle ABD;
    public readonly Triangle ACD;
    public readonly Triangle BCD;

    public Tetrahedron(Point a, Point b, Point c, Point d)
    {
        // Restrict coordinates to guarantee Int128 arithmetic stays within safe bounds
        // and normals are robust. With |coord| <= 2^62-1, any edge delta is <= 2^63-1,
        // and products used in cross/dot remain <= 2^126, safely within Int128 range.
        ValidatePointRange(a);
        ValidatePointRange(b);
        ValidatePointRange(c);
        ValidatePointRange(d);

        // Canonicalize orientation so that 6Ã— volume is positive.
        var ab = Vector128.FromPoints(a, b);
        var ac = Vector128.FromPoints(a, c);
        var ad = Vector128.FromPoints(a, d);
        var vol6 = Vector128.Dot(Vector128.Cross(ab, ac), ad);

        if (vol6 == 0)
        {
            throw new InvalidOperationException("Degenerate tetrahedron (zero volume).");
        }

        if (vol6 < 0)
        {
            // Flip orientation by swapping C and D.
            var tmp = c; c = d; d = tmp;

            ab = Vector128.FromPoints(a, b);
            ac = Vector128.FromPoints(a, c);
            ad = Vector128.FromPoints(a, d);
            vol6 = Vector128.Dot(Vector128.Cross(ab, ac), ad);
            if (vol6 <= 0)
            {
                throw new InvalidOperationException("Failed to produce positively oriented tetrahedron.");
            }
        }

        A = a; B = b; C = c; D = d;
        Volume = (double)vol6 / 6.0;

        // Precompute outward-facing triangle normals.
        ABC = new Triangle(A, B, C, D);
        ABD = new Triangle(A, B, D, C);
        ACD = new Triangle(A, C, D, B);
        BCD = new Triangle(B, C, D, A);
    }

    // Triangle type moved to Geometry.Triangle (standalone) for reuse.

    private static void ValidatePointRange(in Point p)
    {
        const long MaxAbsCoord = (1L << 62) - 1; // 2^62 - 1
        if (p.X < -MaxAbsCoord || p.X > MaxAbsCoord ||
            p.Y < -MaxAbsCoord || p.Y > MaxAbsCoord ||
            p.Z < -MaxAbsCoord || p.Z > MaxAbsCoord)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p),
                $"Point coordinates must be within [-{MaxAbsCoord}, {MaxAbsCoord}] to guarantee precision.");
        }
    }
}
