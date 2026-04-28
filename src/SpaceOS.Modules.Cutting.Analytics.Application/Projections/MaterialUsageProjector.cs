using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Projections;

/// <summary>
/// Projects waste/consumption events into <see cref="DailyMaterialUsage"/> read-models.
/// Idempotent via <see cref="IProjectionIdempotencyGate"/>.
/// </summary>
public sealed class MaterialUsageProjector(
    IProjectionIdempotencyGate gate,
    IAnalyticsQueryRepository repo,
    ILogger<MaterialUsageProjector> logger)
    : IMaterialUsageProjector
{
    /// <inheritdoc/>
    public async Task ProjectAsync(
        Guid tenantId, string materialCode, DateOnly date,
        decimal totalAreaMm2, decimal wasteAreaMm2, int offcutCount,
        Guid eventId, CancellationToken ct)
    {
        if (await gate.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), tenantId, ct)
                .ConfigureAwait(false))
        {
            logger.LogDebug("Duplicate event {EventId} skipped for MaterialUsageProjector.", eventId);
            return;
        }

        var existing = (await repo.GetMaterialUsageAsync(
            tenantId, materialCode, date, date, 0, 1, ct).ConfigureAwait(false)).FirstOrDefault();

        if (existing is not null)
            existing.Update(totalAreaMm2, wasteAreaMm2, offcutCount);
        else
            DailyMaterialUsage.Create(tenantId, materialCode, date, totalAreaMm2, wasteAreaMm2, offcutCount);
    }
}
