using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.IngestOrder;

/// <summary>
/// Ingests an order by creating CuttingJobs for each item and assigning them to the
/// earliest Open DaySlot with available capacity. Idempotent on OrderId.
/// </summary>
public sealed class IngestOrderCommandHandler
    : IRequestHandler<IngestOrderCommand, Result<int>>
{
    private readonly ICuttingRepository _repository;
    private readonly ICapacityModel _capacityModel;

    public IngestOrderCommandHandler(ICuttingRepository repository, ICapacityModel capacityModel)
    {
        _repository = repository;
        _capacityModel = capacityModel;
    }

    public async Task<Result<int>> Handle(IngestOrderCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return Result<int>.Invalid(new ValidationError("Items list must not be empty."));

        // Idempotency: if jobs already exist for this OrderId, return success with 0 new jobs
        if (await _repository.HasJobsForOrderAsync(request.OrderId, ct).ConfigureAwait(false))
            return Result<int>.Success(0);

        var openSlots = await _repository.GetOpenSlotsOrderedByDateAsync(ct).ConfigureAwait(false);
        if (openSlots.Count == 0)
            return Result<int>.Error("No open DaySlots available for scheduling.");

        int created = 0;

        foreach (var item in request.Items)
        {
            // Estimate cutting time from dimensions (area-based: area_m2 / 2.5 h/m2)
            var areaMm2 = item.WidthMm * item.HeightMm;
            var estimatedHours = areaMm2 > 0 ? areaMm2 / (1_000_000m * 2.5m) : 0.5m;
            if (estimatedHours < 0.1m) estimatedHours = 0.1m;

            // Find earliest slot with capacity and create job for it
            bool assigned = false;
            foreach (var slot in openSlots)
            {
                var scheduledDate = slot.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var job = CuttingJob.Create(
                    daySlotId: slot.Id,
                    orderId: request.OrderId,
                    scheduledDate: scheduledDate,
                    priority: "Normal",
                    estimatedTimeHours: estimatedHours,
                    widthMm: item.WidthMm,
                    heightMm: item.HeightMm,
                    material: item.Material,
                    grainDirection: item.GrainDirection);

                var addResult = slot.AddJob(job, _capacityModel);
                if (addResult.IsSuccess)
                {
                    assigned = true;
                    created++;
                    break;
                }
            }

            if (!assigned)
                return Result<int>.Error($"No DaySlot has capacity for item '{item.Name}' ({item.WidthMm}x{item.HeightMm}mm).");
        }

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<int>.Success(created);
    }
}
