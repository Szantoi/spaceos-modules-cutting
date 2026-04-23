using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

/// <summary>Strategy that decides what to do when a CuttingJob cannot fit its assigned DaySlot.</summary>
public interface IReworkPolicy
{
    string PolicyId { get; }

    /// <summary>Evaluates whether the job needs rescheduling for the given target slot.</summary>
    ReworkDecision Evaluate(CuttingJob job, DaySlot targetSlot);

    /// <summary>Executes the rework: moves the job to the first available slot, or marks it as Warning.</summary>
    void Apply(CuttingJob job, IReadOnlyList<DaySlot> availableSlots);
}
