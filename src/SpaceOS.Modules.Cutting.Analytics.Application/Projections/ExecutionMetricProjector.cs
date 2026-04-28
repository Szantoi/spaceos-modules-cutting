using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Projections;

/// <summary>
/// Projects CuttingJobCompleted events into <see cref="DailyExecutionMetric"/> read-models.
/// Idempotent via <see cref="IProjectionIdempotencyGate"/>.
/// </summary>
public sealed class ExecutionMetricProjector(
    IProjectionIdempotencyGate gate,
    IAnalyticsQueryRepository repo,
    ILogger<ExecutionMetricProjector> logger)
    : IExecutionMetricProjector
{
    /// <inheritdoc/>
    public async Task ProjectAsync(
        Guid tenantId, string machineId, DateOnly date,
        int completedCount, decimal avgDurationMinutes, decimal yieldPercent,
        Guid eventId, CancellationToken ct)
    {
        if (await gate.IsAlreadyProcessedAsync(eventId, nameof(ExecutionMetricProjector), tenantId, ct)
                .ConfigureAwait(false))
        {
            logger.LogDebug("Duplicate event {EventId} skipped for ExecutionMetricProjector.", eventId);
            return;
        }

        var existing = (await repo.GetExecutionMetricsAsync(
            tenantId, machineId, date, date, 0, 1, ct).ConfigureAwait(false)).FirstOrDefault();

        if (existing is not null)
            existing.Update(completedCount, avgDurationMinutes, yieldPercent);
        else
            DailyExecutionMetric.Create(tenantId, machineId, date, completedCount, avgDurationMinutes, yieldPercent);
    }
}
