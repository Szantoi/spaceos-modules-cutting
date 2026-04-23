using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>
/// Contract for a pluggable cutting-plan scheduling strategy.
/// Each implementation defines its own job-ordering, slot-allocation, and yield-estimation logic.
/// </summary>
public interface IPlanningStrategy
{
    /// <summary>
    /// Schedules <paramref name="unscheduledJobs"/> into the available <paramref name="daySlots"/>,
    /// returning a (possibly reduced) list of jobs that were successfully allocated.
    /// Jobs that do not fit any slot are omitted from the result.
    /// </summary>
    Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DaySlot> daySlots,
        CancellationToken ct);

    /// <summary>
    /// Calculates the material-yield percentage for the complete plan.
    /// For v1 this is a capacity-utilisation estimate; geometry-based yield is deferred to Phase 3.
    /// </summary>
    decimal CalculateYield(CuttingPlan plan, IEnumerable<DaySlot> daySlots);

    /// <summary>Returns the human-readable display name shown in UI dropdowns.</summary>
    string GetLabel();

    /// <summary>
    /// Validates the <paramref name="plan"/> before scheduling begins.
    /// Async to support future database lookups (tenant strategy config, etc.).
    /// </summary>
    Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct);
}
