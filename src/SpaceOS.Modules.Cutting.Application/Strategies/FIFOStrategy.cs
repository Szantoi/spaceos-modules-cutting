using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>
/// First-In-First-Out scheduling strategy.
/// Jobs are processed in creation order (earliest <see cref="CuttingJob.ScheduledDate"/> first),
/// then allocated to the first slot with sufficient remaining capacity.
/// Target yield: ~70%.
/// </summary>
public sealed class FIFOStrategy : IPlanningStrategy
{
    /// <inheritdoc/>
    public Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DaySlot> daySlots,
        CancellationToken ct)
    {
        var sorted = unscheduledJobs
            .OrderBy(j => j.ScheduledDate)
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
    public string GetLabel() => "FIFO (First-In-First-Out)";

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
