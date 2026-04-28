using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

/// <summary>
/// Overall Equipment Effectiveness score decomposed into Availability × Performance × Quality.
/// All components are in the [0.0, 1.0] range.
/// </summary>
public sealed class OEEScore
{
    /// <summary>Fraction of planned time the machine was actually available (0.0–1.0).</summary>
    public decimal Availability { get; }

    /// <summary>Speed efficiency relative to ideal cycle time (0.0–1.0).</summary>
    public decimal Performance { get; }

    /// <summary>Ratio of good units to total units produced (0.0–1.0).</summary>
    public decimal Quality { get; }

    /// <summary>Composite OEE = Availability × Performance × Quality.</summary>
    public decimal Overall => Availability * Performance * Quality;

    private OEEScore(decimal availability, decimal performance, decimal quality)
    {
        Availability = availability;
        Performance = performance;
        Quality = quality;
    }

    /// <summary>
    /// Creates a validated <see cref="OEEScore"/>.
    /// </summary>
    public static Result<OEEScore> Create(decimal availability, decimal performance, decimal quality)
    {
        if (availability is < 0 or > 1)
            return Result<OEEScore>.Invalid(new ValidationError("Availability must be between 0 and 1."));
        if (performance is < 0 or > 1)
            return Result<OEEScore>.Invalid(new ValidationError("Performance must be between 0 and 1."));
        if (quality is < 0 or > 1)
            return Result<OEEScore>.Invalid(new ValidationError("Quality must be between 0 and 1."));

        return Result<OEEScore>.Success(new OEEScore(availability, performance, quality));
    }
}
