using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>
/// Guillotine-optimised scheduling strategy.
/// Jobs are sorted by material footprint (WidthMm desc, HeightMm desc) then by priority ascending,
/// and allocated to the first daily slot with sufficient remaining capacity.
/// Target yield: 91%+.
/// </summary>
/// <remarks>
/// Because <see cref="CuttingJob"/> does not yet carry panel dimensions (Phase 3),
/// v1 approximates footprint by estimating hours as a proxy for material area.
/// The sort therefore degrades to (EstimatedTimeHours desc, Priority rank asc).
/// </remarks>
public sealed class MaxCutStrategy : IPlanningStrategy
{
    private static readonly Dictionary<string, int> PriorityRank = new()
    {
        ["Urgent"] = 1,
        ["High"]   = 2,
        ["Normal"] = 3,
        ["Low"]    = 4,
    };

    /// <inheritdoc/>
    public Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DailyPlan> dailyPlans,
        CancellationToken ct)
    {
        var sorted = unscheduledJobs
            .OrderByDescending(j => j.EstimatedTimeHours)   // proxy for WidthMm desc, HeightMm desc
            .ThenBy(j => PriorityRank.GetValueOrDefault(j.Priority, 99))
            .ToList();

        var slots = dailyPlans.ToList();
        var allocated = new List<CuttingJob>();
        var remainingCapacity = slots.ToDictionary(d => d.Id, d => d.AvailableCapacity - d.AllocatedCapacity);

        foreach (var job in sorted)
        {
            var slot = slots.FirstOrDefault(d => remainingCapacity.GetValueOrDefault(d.Id) >= job.EstimatedTimeHours);
            if (slot is null) continue;

            var scheduled = CuttingJob.Create(slot.Id, job.OrderId, slot.Date, job.Priority, job.EstimatedTimeHours);
            remainingCapacity[slot.Id] -= job.EstimatedTimeHours;
            allocated.Add(scheduled);
        }

        return Task.FromResult<IEnumerable<CuttingJob>>(allocated);
    }

    /// <inheritdoc/>
    public decimal CalculateYield(CuttingPlan plan, IEnumerable<DailyPlan> dailyPlans)
    {
        var slots = dailyPlans.ToList();
        var totalCapacity = slots.Sum(d => d.AvailableCapacity);
        if (totalCapacity == 0m) return 0m;

        var allocatedHours = slots.Sum(d => d.AllocatedCapacity);
        return Math.Round(allocatedHours / totalCapacity * 100m, 2);
    }

    /// <inheritdoc/>
    public string GetLabel() => "MaxCut v1 (Guillotine Optimization)";

    /// <inheritdoc/>
    public Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct)
    {
        var errors = new List<string>();

        if (!plan.DailyPlans.Any())
            errors.Add("Plan must have at least one daily slot.");

        if (plan.DailyPlans.Any(d => d.AvailableCapacity <= 0m))
            errors.Add("All daily slots must have positive available capacity.");

        var result = errors.Count == 0
            ? PlanningValidationResult.Ok()
            : PlanningValidationResult.Fail(errors.ToArray());

        return Task.FromResult(result);
    }
}
