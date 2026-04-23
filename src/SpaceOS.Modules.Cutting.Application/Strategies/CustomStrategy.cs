using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

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
        IEnumerable<DaySlot> daySlots,
        CancellationToken ct)
        => _fallback.ScheduleJobsAsync(unscheduledJobs, daySlots, ct);

    /// <inheritdoc/>
    public decimal CalculateYield(CuttingPlan plan, IEnumerable<DaySlot> daySlots)
        => _fallback.CalculateYield(plan, daySlots);

    /// <inheritdoc/>
    public string GetLabel() => "Custom (Tenant-Specific)";

    /// <inheritdoc/>
    public Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct)
        => _fallback.ValidateAsync(plan, ct);
}
