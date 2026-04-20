using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>
/// Placeholder strategy for tenant-specific scheduling logic (v1.5+).
/// In v1 all calls are delegated to <see cref="MaxCutStrategy"/>.
/// </summary>
public sealed class CustomStrategy : IPlanningStrategy
{
    private readonly MaxCutStrategy _fallback = new();

    /// <inheritdoc/>
    public Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DailyPlan> dailyPlans,
        CancellationToken ct)
        => _fallback.ScheduleJobsAsync(unscheduledJobs, dailyPlans, ct);

    /// <inheritdoc/>
    public decimal CalculateYield(CuttingPlan plan, IEnumerable<DailyPlan> dailyPlans)
        => _fallback.CalculateYield(plan, dailyPlans);

    /// <inheritdoc/>
    public string GetLabel() => "Custom (Tenant-Specific)";

    /// <inheritdoc/>
    public Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct)
        => _fallback.ValidateAsync(plan, ct);
}
