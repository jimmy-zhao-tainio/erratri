namespace Geometry;

internal readonly record struct Vector128(Int128 X, Int128 Y, Int128 Z)
{
    public static Vector128 FromPoints(in Point origin, in Point target) =>
        new(
            (Int128)target.X - (Int128)origin.X,
            (Int128)target.Y - (Int128)origin.Y,
            (Int128)target.Z - (Int128)origin.Z);

    public bool IsZero => X == 0 && Y == 0 && Z == 0;

    public static Vector128 Cross(in Vector128 left, in Vector128 right) =>
        new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);

    public static Int128 Dot(in Vector128 left, in Vector128 right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;
}
