using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Domain.Services;

/// <summary>
/// Rework policy v1: if the job cannot fit its assigned slot, find the next available open slot.
/// If no slot is available, mark the job as Warning.
/// </summary>
public sealed class WarnAndApplyPolicy : IReworkPolicy
{
    private readonly ICapacityModel _capacityModel;

    public WarnAndApplyPolicy(ICapacityModel capacityModel)
    {
        _capacityModel = capacityModel;
    }

    public string PolicyId => "warn-and-apply-v1";

    /// <inheritdoc/>
    public ReworkDecision Evaluate(CuttingJob job, DaySlot targetSlot)
    {
        if (targetSlot.Status != DaySlotStatus.Open)
            return new ReworkDecision(
                CanReschedule: true,
                TargetSlot: null,
                Reason: $"Slot {targetSlot.Id} is {targetSlot.Status} — cannot accept new jobs.");

        if (!_capacityModel.HasCapacity(targetSlot, job))
            return new ReworkDecision(
                CanReschedule: true,
                TargetSlot: null,
                Reason: $"Slot {targetSlot.Id} has insufficient capacity for job {job.Id}.");

        return new ReworkDecision(
            CanReschedule: false,
            TargetSlot: targetSlot,
            Reason: "Slot is open and has sufficient capacity.");
    }

    /// <inheritdoc/>
    public void Apply(CuttingJob job, IReadOnlyList<DaySlot> availableSlots)
    {
        var targetSlot = availableSlots
            .FirstOrDefault(s => s.Status == DaySlotStatus.Open && _capacityModel.HasCapacity(s, job));

        if (targetSlot is null)
        {
            job.MarkAsWarning();
            return;
        }

        job.RescheduleTo(targetSlot.Id);
    }
}
