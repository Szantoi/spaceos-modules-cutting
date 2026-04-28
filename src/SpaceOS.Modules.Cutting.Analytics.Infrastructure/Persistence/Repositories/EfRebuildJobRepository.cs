using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRebuildJobRepository"/>.
/// </summary>
public sealed class EfRebuildJobRepository(CuttingAnalyticsDbContext db) : IRebuildJobRepository
{
    /// <inheritdoc/>
    public async Task<AnalyticsRebuildJob?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.AnalyticsRebuildJobs
            .FirstOrDefaultAsync(j => j.Id == id, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<AnalyticsRebuildJob?> GetActiveForTenantAsync(Guid tenantId, CancellationToken ct)
        => await db.AnalyticsRebuildJobs
            .Where(j => j.TenantId == tenantId
                        && (j.Status == RebuildJobStatus.Pending || j.Status == RebuildJobStatus.Running))
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(AnalyticsRebuildJob job, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(job);
        await db.AnalyticsRebuildJobs.AddAsync(job, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct).ConfigureAwait(false);
}
