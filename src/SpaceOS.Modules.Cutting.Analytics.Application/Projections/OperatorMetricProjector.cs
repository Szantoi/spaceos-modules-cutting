using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Application.Projections;

/// <summary>
/// Projects worker-execution events into <see cref="DailyOperatorMetric"/> read-models,
/// applying SEC-06 k-anonymity suppression when the rolling window has fewer than k distinct workers.
/// Idempotent via <see cref="IProjectionIdempotencyGate"/>.
/// </summary>
public sealed class OperatorMetricProjector(
    IProjectionIdempotencyGate gate,
    IAnalyticsQueryRepository repo,
    ILogger<OperatorMetricProjector> logger)
    : IOperatorMetricProjector
{
    private static readonly AnonymizationPolicy Policy = AnonymizationPolicy.Default;

    /// <inheritdoc/>
    public async Task ProjectAsync(
        Guid tenantId, Guid workerId, DateOnly date,
        int completedExecutions, decimal avgDurationMinutes,
        Guid eventId, CancellationToken ct)
    {
        if (await gate.IsAlreadyProcessedAsync(eventId, nameof(OperatorMetricProjector), tenantId, ct)
                .ConfigureAwait(false))
        {
            logger.LogDebug("Duplicate event {EventId} skipped for OperatorMetricProjector.", eventId);
            return;
        }

        // SEC-06: evaluate k-anonymity over a rolling MinDaysWindow
        var windowStart = date.AddDays(-Policy.MinDaysWindow);
        var existing = await repo.GetOperatorMetricsAnonymizedAsync(
            tenantId, windowStart, date, Policy, 0, 500, ct).ConfigureAwait(false);

        // Count distinct non-suppressed workers in the window
        var distinctWorkers = existing
            .Select(m => m.WorkerId)
            .Where(w => w.HasValue)
            .Distinct()
            .Count();

        // Include the incoming worker if not already in the window
        if (!existing.Any(m => m.WorkerId == workerId))
            distinctWorkers++;

        var metric = DailyOperatorMetric.Create(tenantId, workerId, date, completedExecutions, avgDurationMinutes);

        // Suppress identity when below k-threshold
        if (distinctWorkers < Policy.KThreshold)
        {
            metric.Suppress();
            logger.LogInformation(
                "Operator metric suppressed for tenant {TenantId}: distinct workers {Count} < k={K}",
                tenantId, distinctWorkers, Policy.KThreshold);
        }
    }
}
