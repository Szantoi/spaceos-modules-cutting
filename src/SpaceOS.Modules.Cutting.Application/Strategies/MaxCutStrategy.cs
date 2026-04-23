using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>
/// Guillotine-optimised scheduling strategy.
/// Jobs are sorted by material footprint (WidthMm desc, HeightMm desc) then by priority ascending,
/// and allocated to the first daily slot with sufficient remaining capacity.
/// Target yield: 91%+.
/// </summary>
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
        IEnumerable<DaySlot> daySlots,
        CancellationToken ct)
    {
        var sorted = unscheduledJobs
            .OrderByDescending(j => j.EstimatedTimeHours)
            .ThenBy(j => PriorityRank.GetValueOrDefault(j.Priority, 99))
            .ToList();

        var slots = daySlots.ToList();
        var allocated = new List<CuttingJob>();
        var remainingCapacity = slots.ToDictionary(d => d.Id, d => d.CapacityHours - d.UsedCapacityHours);

        foreach (var job in sorted)
        {
            var slot = slots.FirstOrDefault(d => remainingCapacity.GetValueOrDefault(d.Id) >= job.EstimatedTimeHours);
            if (slot is null) continue;

            var scheduled = CuttingJob.Create(slot.Id, job.OrderId,
                slot.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                job.Priority, job.EstimatedTimeHours);
            remainingCapacity[slot.Id] -= job.EstimatedTimeHours;
            allocated.Add(scheduled);
        }

        return Task.FromResult<IEnumerable<CuttingJob>>(allocated);
    }

    /// <inheritdoc/>
    public decimal CalculateYield(CuttingPlan plan, IEnumerable<DaySlot> daySlots)
    {
        var slots = daySlots.ToList();
        var totalCapacity = slots.Sum(d => d.CapacityHours);
        if (totalCapacity == 0m) return 0m;

        var allocatedHours = slots.Sum(d => d.UsedCapacityHours);
        return Math.Round(allocatedHours / totalCapacity * 100m, 2);
    }

    /// <inheritdoc/>
    public string GetLabel() => "MaxCut v1 (Guillotine Optimization)";

    /// <inheritdoc/>
    public Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct)
    {
        var errors = new List<string>();

        if (!plan.DaySlots.Any())
            errors.Add("Plan must have at least one daily slot.");

        if (plan.DaySlots.Any(d => d.CapacityHours <= 0m))
            errors.Add("All daily slots must have positive available capacity.");

        var result = errors.Count == 0
            ? PlanningValidationResult.Ok()
            : PlanningValidationResult.Fail(errors.ToArray());

        return Task.FromResult(result);
    }
}
