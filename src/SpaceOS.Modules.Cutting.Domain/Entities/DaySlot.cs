using Ardalis.Result;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>Child entity of CuttingPlan representing one day's scheduled cutting work.</summary>
public sealed class DaySlot
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public DateOnly SlotDate { get; private set; }
    public DaySlotStatus Status { get; private set; } = DaySlotStatus.Open;
    public decimal CapacityHours { get; private set; }
    public decimal UsedCapacityHours { get; private set; }

    private readonly List<CuttingJob> _jobs = new();
    public IReadOnlyList<CuttingJob> Jobs => _jobs.AsReadOnly();

    /// <summary>Computed utilisation: UsedCapacityHours / CapacityHours × 100.</summary>
    public decimal UtilizationPercent => CapacityHours > 0 ? UsedCapacityHours / CapacityHours * 100 : 0;

    private DaySlot() { }

    /// <summary>Creates a DaySlot for the given date with the default 8-hour machine capacity.</summary>
    public static DaySlot Create(Guid cuttingPlanId, DateOnly slotDate, decimal capacityHours = 8m)
    {
        if (cuttingPlanId == Guid.Empty) throw new ArgumentException("CuttingPlanId required.", nameof(cuttingPlanId));
        if (capacityHours <= 0) throw new ArgumentException("CapacityHours must be positive.", nameof(capacityHours));

        return new DaySlot
        {
            Id = Guid.NewGuid(),
            CuttingPlanId = cuttingPlanId,
            SlotDate = slotDate,
            Status = DaySlotStatus.Open,
            CapacityHours = capacityHours,
            UsedCapacityHours = 0m
        };
    }

    /// <summary>Transitions this slot from Open → Locked (idempotent: already-Locked returns Success).</summary>
    public Result Lock()
    {
        if (Status == DaySlotStatus.Locked) return Result.Success();
        if (Status == DaySlotStatus.Closed)
            return Result.Invalid(new ValidationError($"DaySlot {Id} cannot be locked: already Closed."));
        Status = DaySlotStatus.Locked;
        return Result.Success();
    }

    /// <summary>Transitions this slot from Locked → Closed.</summary>
    public Result CloseSlot()
    {
        if (Status != DaySlotStatus.Locked)
            return Result.Invalid(new ValidationError($"DaySlot {Id} cannot be closed from status '{Status}'."));
        Status = DaySlotStatus.Closed;
        return Result.Success();
    }

    /// <summary>Adds a job to this slot after verifying status, FK, and remaining capacity.</summary>
    public Result AddJob(CuttingJob job, ICapacityModel capacityModel)
    {
        if (Status != DaySlotStatus.Open)
            return Result.Invalid(new ValidationError($"DaySlot {Id} is not Open (current status: {Status})."));
        if (job.DaySlotId != Id)
            return Result.Invalid(new ValidationError("Job belongs to a different day slot."));
        if (!capacityModel.HasCapacity(this, job))
            return Result.Invalid(new ValidationError(
                $"Insufficient capacity: {CapacityHours - UsedCapacityHours:F2}h remaining."));

        _jobs.Add(job);
        UsedCapacityHours += capacityModel.ComputeJobCost(job);
        return Result.Success();
    }
}
