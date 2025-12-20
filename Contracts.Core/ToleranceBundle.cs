namespace Contracts.Core;

public sealed record ToleranceBundle
{
    public required double DistanceEpsilon { get; init; }
    public required double AngleEpsilonRadians { get; init; }
    public required double AreaEpsilon { get; init; }

    public void ValidateStrict(ContractValidationContext ctx, string pathPrefix = "tolerances")
    {
        ctx.Require(DistanceEpsilon > 0 && double.IsFinite(DistanceEpsilon),
            ContractErrorCode.From("CORE.TOLERANCE.DISTANCE_EPSILON_INVALID"),
            "DistanceEpsilon must be finite and > 0.",
            $"{pathPrefix}.DistanceEpsilon");

        ctx.Require(AngleEpsilonRadians > 0 && double.IsFinite(AngleEpsilonRadians),
            ContractErrorCode.From("CORE.TOLERANCE.ANGLE_EPSILON_RADIANS_INVALID"),
            "AngleEpsilonRadians must be finite and > 0.",
            $"{pathPrefix}.AngleEpsilonRadians");

        ctx.Require(AreaEpsilon > 0 && double.IsFinite(AreaEpsilon),
            ContractErrorCode.From("CORE.TOLERANCE.AREA_EPSILON_INVALID"),
            "AreaEpsilon must be finite and > 0.",
            $"{pathPrefix}.AreaEpsilon");
    }
}
