using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>Persistence contract for <see cref="AnalyticsRebuildJob"/> aggregates.</summary>
public interface IRebuildJobRepository
{
    /// <summary>Returns the job with the given ID, or null if not found.</summary>
    Task<AnalyticsRebuildJob?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Returns the Pending or Running job for the tenant, or null if none exists.</summary>
    Task<AnalyticsRebuildJob?> GetActiveForTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Persists a new job to the store.</summary>
    Task AddAsync(AnalyticsRebuildJob job, CancellationToken ct);

    /// <summary>Flushes pending changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
