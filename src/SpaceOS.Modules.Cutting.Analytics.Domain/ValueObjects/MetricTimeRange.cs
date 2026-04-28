using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

/// <summary>
/// A validated time range with resolution hint for analytics queries.
/// Maximum span is 365 days.
/// </summary>
public sealed class MetricTimeRange
{
    /// <summary>Inclusive start of the range (UTC).</summary>
    public DateTimeOffset From { get; }

    /// <summary>Exclusive end of the range (UTC).</summary>
    public DateTimeOffset To { get; }

    /// <summary>Desired granularity of the returned data points.</summary>
    public MetricResolution Resolution { get; }

    private MetricTimeRange(DateTimeOffset from, DateTimeOffset to, MetricResolution resolution)
    {
        From = from;
        To = to;
        Resolution = resolution;
    }

    /// <summary>Creates and validates a <see cref="MetricTimeRange"/>.</summary>
    public static Result<MetricTimeRange> Create(DateTimeOffset from, DateTimeOffset to, MetricResolution resolution)
    {
        if (from >= to)
            return Result<MetricTimeRange>.Invalid(new ValidationError("From must be before To."));

        var span = to - from;
        if (span.TotalDays > 365)
            return Result<MetricTimeRange>.Invalid(new ValidationError("Time range cannot exceed 365 days."));

        return Result<MetricTimeRange>.Success(new MetricTimeRange(from, to, resolution));
    }
}
