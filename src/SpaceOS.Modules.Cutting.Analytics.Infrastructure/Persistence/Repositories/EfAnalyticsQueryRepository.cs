using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAnalyticsQueryRepository"/>.
/// All queries use <c>AsNoTracking</c> — this is a read-side repository.
/// </summary>
public sealed class EfAnalyticsQueryRepository(CuttingAnalyticsDbContext db)
    : IAnalyticsQueryRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<DailyExecutionMetric>> GetExecutionMetricsAsync(
        Guid tenantId, string? machineId, DateOnly from, DateOnly to,
        int skip, int take, CancellationToken ct)
    {
        var q = db.DailyExecutionMetrics.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.MetricDate >= from && m.MetricDate <= to);

        if (machineId is not null)
            q = q.Where(m => m.MachineId == machineId);

        return await q
            .OrderBy(m => m.MetricDate).ThenBy(m => m.MachineId)
            .Skip(skip).Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DailyMaterialUsage>> GetMaterialUsageAsync(
        Guid tenantId, string? materialCode, DateOnly from, DateOnly to,
        int skip, int take, CancellationToken ct)
    {
        var q = db.DailyMaterialUsages.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.UsageDate >= from && m.UsageDate <= to);

        if (materialCode is not null)
            q = q.Where(m => m.MaterialCode == materialCode);

        return await q
            .OrderBy(m => m.UsageDate).ThenBy(m => m.MaterialCode)
            .Skip(skip).Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MachineOEEHourly>> GetOEEAsync(
        Guid tenantId, string? machineId, DateTime from, DateTime to,
        int skip, int take, CancellationToken ct)
    {
        var q = db.MachineOEEHourlies.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.HourSlot >= from && m.HourSlot <= to);

        if (machineId is not null)
            q = q.Where(m => m.MachineId == machineId);

        return await q
            .OrderBy(m => m.HourSlot).ThenBy(m => m.MachineId)
            .Skip(skip).Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DailyOperatorMetric>> GetOperatorMetricsAnonymizedAsync(
        Guid tenantId, DateOnly from, DateOnly to,
        AnonymizationPolicy policy, int skip, int take, CancellationToken ct)
    {
        // Only non-suppressed records are returned (SEC-06).
        return await db.DailyOperatorMetrics.AsNoTracking()
            .Where(m => m.TenantId == tenantId
                        && m.MetricDate >= from
                        && m.MetricDate <= to
                        && !m.IsSuppressed)
            .OrderBy(m => m.MetricDate)
            .Skip(skip).Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AnalyticsRebuildJob?> GetRebuildJobAsync(Guid jobId, CancellationToken ct)
        => await db.AnalyticsRebuildJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct).ConfigureAwait(false);
}
