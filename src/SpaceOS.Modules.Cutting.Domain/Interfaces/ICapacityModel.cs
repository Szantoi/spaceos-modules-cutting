using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface ICapacityModel
{
    string ModelId { get; }

    /// <summary>Total capacity in machine-hours available for this slot.</summary>
    decimal ComputeCapacityHours(DaySlot slot);

    /// <summary>Cost in machine-hours of scheduling the given job.</summary>
    decimal ComputeJobCost(CuttingJob job);

    /// <summary>Returns true if the slot has sufficient remaining capacity for the job.</summary>
    bool HasCapacity(DaySlot slot, CuttingJob job);
}
