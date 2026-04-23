using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using System.Text.Json;

namespace SpaceOS.Modules.Cutting.Application.EventHandlers;

/// <summary>
/// Listens to CuttingPlanFrozenNotification and registers eligible waste pieces as offcuts in Inventory.
/// Threshold: only WastePieces where WidthMm >= 400 and HeightMm >= 400.
/// Best-effort: errors are logged but not propagated.
/// </summary>
public sealed class RegisterOffcutsOnPlanFrozenHandler : INotificationHandler<CuttingPlanFrozenNotification>
{
    private const decimal MinDimensionMm = 400m;

    private readonly IPlanNestingSnapshotRepository _snapshotRepo;
    private readonly IInventoryCuttingAdapter _inventoryAdapter;
    private readonly ILogger<RegisterOffcutsOnPlanFrozenHandler> _logger;

    public RegisterOffcutsOnPlanFrozenHandler(
        IPlanNestingSnapshotRepository snapshotRepo,
        IInventoryCuttingAdapter inventoryAdapter,
        ILogger<RegisterOffcutsOnPlanFrozenHandler> logger)
    {
        _snapshotRepo = snapshotRepo;
        _inventoryAdapter = inventoryAdapter;
        _logger = logger;
    }

    public async Task Handle(CuttingPlanFrozenNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _snapshotRepo.GetByPlanAsync(notification.PlanId, cancellationToken)
                .ConfigureAwait(false);

            if (snapshot is null)
            {
                _logger.LogWarning(
                    "RegisterOffcutsOnPlanFrozenHandler: no nesting snapshot for plan {PlanId}. Offcut registration skipped.",
                    notification.PlanId);
                return;
            }

            List<PanelAssignmentSnapshot>? assignments;
            try
            {
                assignments = JsonSerializer.Deserialize<List<PanelAssignmentSnapshot>>(
                    snapshot.NestingResultJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RegisterOffcutsOnPlanFrozenHandler: failed to deserialize snapshot for plan {PlanId}.",
                    notification.PlanId);
                return;
            }

            if (assignments is null || assignments.Count == 0)
                return;

            var offcutItems = assignments
                .SelectMany(a => (a.WastePieces ?? [])
                    .Where(w => w.WidthMm >= MinDimensionMm && w.HeightMm >= MinDimensionMm)
                    .Select(w => new OffcutRegistrationItem(
                        MaterialCode: a.MaterialCode,
                        WidthMm: w.WidthMm,
                        HeightMm: w.HeightMm,
                        X: w.X,
                        Y: w.Y)))
                .ToList();

            if (offcutItems.Count == 0)
            {
                _logger.LogInformation(
                    "RegisterOffcutsOnPlanFrozenHandler: no waste pieces above threshold for plan {PlanId}.",
                    notification.PlanId);
                return;
            }

            var result = await _inventoryAdapter.RegisterOffcutsAsync(
                notification.PlanId, notification.TenantId, offcutItems, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "RegisterOffcutsOnPlanFrozenHandler: RegisterOffcutsAsync failed for plan {PlanId}: {Errors}",
                    notification.PlanId, string.Join("; ", result.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "RegisterOffcutsOnPlanFrozenHandler: registered {Count} offcuts for plan {PlanId}.",
                    offcutItems.Count, notification.PlanId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RegisterOffcutsOnPlanFrozenHandler: unexpected error for plan {PlanId}. Best-effort — not propagating.",
                notification.PlanId);
        }
    }
}

/// <summary>Internal DTO for deserializing nesting snapshot JSON.</summary>
internal sealed class PanelAssignmentSnapshot
{
    public string MaterialCode { get; set; } = string.Empty;
    public List<WastePieceSnapshot>? WastePieces { get; set; }
}

internal sealed class WastePieceSnapshot
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
}
