using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Contracts.Inventory;
using SpaceOS.Modules.Contracts.Inventory.Requests;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.ReservePanels;

/// <summary>
/// Reserves panels in the Inventory service for every job in the CuttingPlan.
/// On partial failure, rolls back already-created reservations (Release).
/// Returns the count of created reservations on success.
/// </summary>
public sealed class ReservePanelsCommandHandler
    : IRequestHandler<ReservePanelsCommand, Result<int>>
{
    private readonly ICuttingRepository _cuttingRepo;
    private readonly IPanelReservationRepository _reservationRepo;
    private readonly IInventoryProvider _inventoryProvider;

    public ReservePanelsCommandHandler(
        ICuttingRepository cuttingRepo,
        IPanelReservationRepository reservationRepo,
        IInventoryProvider inventoryProvider)
    {
        _cuttingRepo = cuttingRepo;
        _reservationRepo = reservationRepo;
        _inventoryProvider = inventoryProvider;
    }

    public async Task<Result<int>> Handle(ReservePanelsCommand request, CancellationToken ct)
    {
        var plan = await _cuttingRepo.GetCuttingPlanByIdAsync(request.PlanId, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<int>.NotFound($"CuttingPlan {request.PlanId} not found.");

        var created = new List<(Guid CorrelationId, PanelReservation Reservation)>();

        foreach (var slot in plan.DaySlots)
        {
            foreach (var job in slot.Jobs)
            {
                var widthMm  = job.WidthMm  > 0 ? job.WidthMm  : 1200m;
                var heightMm = job.HeightMm > 0 ? job.HeightMm : 800m;
                var materialCode = $"panel-{slot.SlotDate:yyyy-MM-dd}-{job.Id:N}"[..42];
                var areaMm2 = widthMm * heightMm;

                // Generate correlation ID (idempotency key per job reservation attempt)
                var correlationId = Guid.NewGuid();

                var reserveRequest = new ReserveStockRequest(
                    CorrelationId: correlationId,
                    ConsumerModule: "Cutting",
                    ConsumerContextJson: null,
                    Items:
                    [
                        new ReserveItemRequest(
                            StockItemId: job.Id,
                            MaterialCode: materialCode,
                            QuantityReserved: areaMm2)
                    ],
                    Ttl: TimeSpan.FromHours(24));

                Result<SpaceOS.Modules.Contracts.Inventory.DTOs.ReservationDto> reserveResult;
                try
                {
                    reserveResult = await _inventoryProvider.ReserveAsync(reserveRequest, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await RollbackAsync(created, ct).ConfigureAwait(false);
                    return Result<int>.Error($"Inventory reservation failed for job {job.Id}. All reservations rolled back.");
                }

                if (!reserveResult.IsSuccess)
                {
                    await RollbackAsync(created, ct).ConfigureAwait(false);
                    return Result<int>.Error($"Inventory reservation failed for job {job.Id}: {string.Join("; ", reserveResult.Errors)}. All reservations rolled back.");
                }

                var reservation = PanelReservation.Create(
                    request.TenantId, plan.Id, slot.Id,
                    reserveResult.Value.Id, materialCode, widthMm, heightMm);

                await _reservationRepo.AddAsync(reservation, ct).ConfigureAwait(false);
                created.Add((correlationId, reservation));
            }
        }

        await _reservationRepo.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<int>.Success(created.Count);
    }

    private async Task RollbackAsync(
        IReadOnlyList<(Guid CorrelationId, PanelReservation Reservation)> entries,
        CancellationToken ct)
    {
        foreach (var (correlationId, reservation) in entries)
        {
            try
            {
                await _inventoryProvider.ReleaseReservationAsync(correlationId, "rollback", ct)
                    .ConfigureAwait(false);
                reservation.Release();
            }
            catch
            {
                // Best-effort rollback — log in production
            }
        }
    }
}
