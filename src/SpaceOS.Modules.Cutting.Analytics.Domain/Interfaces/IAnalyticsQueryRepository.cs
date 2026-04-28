using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>Read-side repository for analytics queries and projector upserts.</summary>
public interface IAnalyticsQueryRepository
{
    /// <summary>Returns execution metrics filtered by tenant, optional machine, and date range.</summary>
    Task<IReadOnlyList<DailyExecutionMetric>> GetExecutionMetricsAsync(
        Guid tenantId, string? machineId, DateOnly from, DateOnly to,
        int skip, int take, CancellationToken ct);

    /// <summary>Returns material usage records filtered by tenant, optional material code, and date range.</summary>
    Task<IReadOnlyList<DailyMaterialUsage>> GetMaterialUsageAsync(
        Guid tenantId, string? materialCode, DateOnly from, DateOnly to,
        int skip, int take, CancellationToken ct);

    /// <summary>Returns hourly OEE snapshots filtered by tenant, optional machine, and UTC time range.</summary>
    Task<IReadOnlyList<MachineOEEHourly>> GetOEEAsync(
        Guid tenantId, string? machineId, DateTime from, DateTime to,
        int skip, int take, CancellationToken ct);

    /// <summary>
    /// Returns non-suppressed operator metrics for the given tenant and date range,
    /// applying the provided <paramref name="policy"/> to filter out sub-threshold records.
    /// </summary>
    Task<IReadOnlyList<DailyOperatorMetric>> GetOperatorMetricsAnonymizedAsync(
        Guid tenantId, DateOnly from, DateOnly to,
        AnonymizationPolicy policy, int skip, int take, CancellationToken ct);

    /// <summary>Returns the rebuild job by its ID, or null if not found.</summary>
    Task<AnalyticsRebuildJob?> GetRebuildJobAsync(Guid jobId, CancellationToken ct);
}
