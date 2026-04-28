using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

/// <summary>
/// k-anonymity and l-diversity thresholds used to suppress operator metrics
/// when the group size is too small to prevent re-identification (SEC-06).
/// </summary>
public sealed class AnonymizationPolicy
{
    /// <summary>Minimum group size required before data is released (k-anonymity).</summary>
    public int KThreshold { get; }

    /// <summary>Minimum number of distinct sensitive values in the group (l-diversity).</summary>
    public int LDiversityMin { get; }

    /// <summary>Minimum number of days in the rolling window for evaluation.</summary>
    public int MinDaysWindow { get; }

    private AnonymizationPolicy(int kThreshold, int lDiversityMin, int minDaysWindow)
    {
        KThreshold = kThreshold;
        LDiversityMin = lDiversityMin;
        MinDaysWindow = minDaysWindow;
    }

    /// <summary>Creates and validates an <see cref="AnonymizationPolicy"/>.</summary>
    public static Result<AnonymizationPolicy> Create(int kThreshold, int lDiversityMin, int minDaysWindow)
    {
        if (kThreshold < 2)
            return Result<AnonymizationPolicy>.Invalid(new ValidationError("KThreshold must be at least 2."));
        if (lDiversityMin < 1)
            return Result<AnonymizationPolicy>.Invalid(new ValidationError("LDiversityMin must be at least 1."));
        if (minDaysWindow < 1)
            return Result<AnonymizationPolicy>.Invalid(new ValidationError("MinDaysWindow must be at least 1."));

        return Result<AnonymizationPolicy>.Success(new AnonymizationPolicy(kThreshold, lDiversityMin, minDaysWindow));
    }

    /// <summary>Production-safe default: k=5, l=2, 7-day window.</summary>
    public static AnonymizationPolicy Default => new(kThreshold: 5, lDiversityMin: 2, minDaysWindow: 7);
}
