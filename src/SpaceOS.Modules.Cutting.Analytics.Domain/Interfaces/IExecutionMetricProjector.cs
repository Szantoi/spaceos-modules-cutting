namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>Projects CuttingJobCompleted events into <c>DailyExecutionMetric</c> read-models.</summary>
public interface IExecutionMetricProjector
{
    /// <summary>Upserts the metric row for the given machine/date; idempotent via event dedup.</summary>
    Task ProjectAsync(
        Guid tenantId, string machineId, DateOnly date,
        int completedCount, decimal avgDurationMinutes, decimal yieldPercent,
        Guid eventId, CancellationToken ct);
}
