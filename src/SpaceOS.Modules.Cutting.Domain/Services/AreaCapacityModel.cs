using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Domain.Services;

/// <summary>
/// Time-based capacity model for v1.
/// When part dimensions are available (Phase 3+), computes cost as area / throughput.
/// Falls back to <see cref="CuttingJob.EstimatedTimeHours"/> when dimensions are not yet set.
/// </summary>
public sealed class AreaCapacityModel : ICapacityModel
{
    private readonly decimal _throughputM2PerHour;

    public AreaCapacityModel(decimal throughputM2PerHour = 2.5m)
    {
        if (throughputM2PerHour <= 0)
            throw new ArgumentException("Throughput must be positive.", nameof(throughputM2PerHour));
        _throughputM2PerHour = throughputM2PerHour;
    }

    public string ModelId => "area-v1";

    /// <inheritdoc/>
    public decimal ComputeCapacityHours(DaySlot slot) => slot.CapacityHours;

    /// <inheritdoc/>
    public decimal ComputeJobCost(CuttingJob job)
    {
        // v1 fallback: use EstimatedTimeHours when geometry is not yet available (Phase 3 fills dimensions)
        if (job.WidthMm == 0m || job.HeightMm == 0m)
            return job.EstimatedTimeHours;

        return (job.WidthMm * job.HeightMm / 1_000_000m) / _throughputM2PerHour;
    }

    /// <inheritdoc/>
    public bool HasCapacity(DaySlot slot, CuttingJob job)
        => slot.UsedCapacityHours + ComputeJobCost(job) <= slot.CapacityHours;
}
