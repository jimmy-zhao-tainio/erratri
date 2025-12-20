using System.Globalization;

namespace Contracts.Core;

/// <summary>
/// Helpers for producing deterministic “golden” text outputs in contract conformance tests.
/// </summary>
/// <remarks>
/// <para>
/// This type performs no implicit ordering. Any ordering must be done by contract-specific serializers using
/// deterministic rules (stable sort keys, canonical iteration order) before producing text.
/// </para>
/// <para>
/// Newline normalization is the only implicit normalization provided here. Quantization/rounding is opt-in via
/// <see cref="Quantize"/> and must be explicit and contract-defined (no tolerance-driven magic).
/// </para>
/// </remarks>
public static class GoldenNormalization
{
    public static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");

    public static double Quantize(double value, double step, MidpointRounding rounding = MidpointRounding.AwayFromZero)
    {
        if (!double.IsFinite(value)) return value;
        if (!(step > 0) || !double.IsFinite(step))
            throw new ArgumentOutOfRangeException(nameof(step), "Quantization step must be finite and > 0.");

        var q = value / step;
        var rounded = Math.Round(q, 0, rounding);
        return rounded * step;
    }

    public static string FormatDouble(double value, int maxFractionDigits = 9)
    {
        if (!double.IsFinite(value))
            return value.ToString(CultureInfo.InvariantCulture);

        var format = "0." + new string('#', Math.Max(0, maxFractionDigits));
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
