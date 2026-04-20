using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>
/// Contract for a pluggable cutting-plan scheduling strategy.
/// Each implementation defines its own job-ordering, slot-allocation, and yield-estimation logic.
/// </summary>
public interface IPlanningStrategy
{
    /// <summary>
    /// Schedules <paramref name="unscheduledJobs"/> into the available <paramref name="dailyPlans"/> slots,
    /// returning a (possibly reduced) list of jobs that were successfully allocated.
    /// Jobs that do not fit any slot are omitted from the result.
    /// </summary>
    /// <param name="unscheduledJobs">Jobs that have not yet been assigned to a daily slot.</param>
    /// <param name="dailyPlans">Available daily slots with their remaining capacity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The subset of jobs that were allocated, each with an updated <see cref="CuttingJob.DailyPlanId"/>.</returns>
    Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DailyPlan> dailyPlans,
        CancellationToken ct);

    /// <summary>
    /// Calculates the material-yield percentage for the complete plan.
    /// For v1 this is a capacity-utilisation estimate; geometry-based yield is deferred to Phase 3.
    /// </summary>
    /// <param name="plan">The <see cref="CuttingPlan"/> being evaluated.</param>
    /// <param name="dailyPlans">The daily slots after scheduling has been applied.</param>
    /// <returns>Yield as a percentage (0–100), rounded to two decimal places.</returns>
    decimal CalculateYield(CuttingPlan plan, IEnumerable<DailyPlan> dailyPlans);

    /// <summary>Returns the human-readable display name shown in UI dropdowns.</summary>
    string GetLabel();

    /// <summary>
    /// Validates the <paramref name="plan"/> before scheduling begins.
    /// Async to support future database lookups (tenant strategy config, etc.).
    /// </summary>
    Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct);
}
