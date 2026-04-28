using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Projections;

/// <summary>
/// Projects execution telemetry into <see cref="MachineOEEHourly"/> read-models.
/// Idempotent via <see cref="IProjectionIdempotencyGate"/>.
/// </summary>
public sealed class OEEProjector(
    IProjectionIdempotencyGate gate,
    IAnalyticsQueryRepository repo,
    ILogger<OEEProjector> logger)
    : IOEEProjector
{
    /// <inheritdoc/>
    public async Task ProjectAsync(
        Guid tenantId, string machineId, DateTime hourSlot,
        OEEScore score, Guid eventId, CancellationToken ct)
    {
        if (await gate.IsAlreadyProcessedAsync(eventId, nameof(OEEProjector), tenantId, ct)
                .ConfigureAwait(false))
        {
            logger.LogDebug("Duplicate event {EventId} skipped for OEEProjector.", eventId);
            return;
        }

        var from = hourSlot;
        var to = hourSlot.AddHours(1);
        var existing = (await repo.GetOEEAsync(
            tenantId, machineId, from, to, 0, 1, ct).ConfigureAwait(false)).FirstOrDefault();

        if (existing is not null)
            existing.Update(score);
        else
            MachineOEEHourly.Create(tenantId, machineId, hourSlot, score);
    }
}
