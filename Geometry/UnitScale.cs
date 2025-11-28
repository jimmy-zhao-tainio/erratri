namespace Geometry;

public enum UnitScale
{
    Micrometer,
    Millimeter
}

public static class UnitScaleExtensions
{
    public static double ToMetersFactor(this UnitScale unit) => unit switch
    {
        UnitScale.Micrometer => 1e-6,
        UnitScale.Millimeter => 1e-3,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported unit scale.")
    };

    public static double ToMillimetersFactor(this UnitScale unit) => unit switch
    {
        UnitScale.Micrometer => 0.001,
        UnitScale.Millimeter => 1.0,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported unit scale.")
    };

    public static string Abbreviation(this UnitScale unit) => unit switch
    {
        UnitScale.Micrometer => "µm",
        UnitScale.Millimeter => "mm",
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported unit scale.")
    };
}
