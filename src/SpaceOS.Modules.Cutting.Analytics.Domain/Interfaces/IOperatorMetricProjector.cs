namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>
/// Projects worker-execution events into <c>DailyOperatorMetric</c> read-models,
/// applying SEC-06 k-anonymity suppression when the group is too small.
/// </summary>
public interface IOperatorMetricProjector
{
    /// <summary>Upserts (or suppresses) the metric row for the given worker/date; idempotent via event dedup.</summary>
    Task ProjectAsync(
        Guid tenantId, Guid workerId, DateOnly date,
        int completedExecutions, decimal avgDurationMinutes,
        Guid eventId, CancellationToken ct);
}
