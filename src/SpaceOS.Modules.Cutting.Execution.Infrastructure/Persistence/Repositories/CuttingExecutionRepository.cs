using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICuttingExecutionRepository"/>.
/// Uses <see cref="CuttingDbContext"/> from the shared Infrastructure layer.
/// </summary>
public sealed class CuttingExecutionRepository(CuttingDbContext db) : ICuttingExecutionRepository
{
    private static readonly CuttingExecutionStatus[] _activeStatuses =
    [
        CuttingExecutionStatus.Scheduled,
        CuttingExecutionStatus.Started,
        CuttingExecutionStatus.InProgress
    ];

    /// <inheritdoc />
    public async Task<CuttingExecution?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.CuttingExecutions
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    /// <remarks>Uses AsSplitQuery to avoid Cartesian explosion when loading owned collections.</remarks>
    public async Task<CuttingExecution?> GetByIdWithProgressAsync(Guid id, CancellationToken ct)
        => await db.CuttingExecutions
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddAsync(CuttingExecution execution, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(execution);
        await db.CuttingExecutions.AddAsync(execution, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct).ConfigureAwait(false);

    /// <summary>Returns all executions for a tenant, newest first.</summary>
    public async Task<IReadOnlyList<CuttingExecution>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
        => await db.CuttingExecutions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ScheduledAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <summary>Returns non-terminal executions for a tenant.</summary>
    public async Task<IReadOnlyList<CuttingExecution>> ListActiveByTenantAsync(Guid tenantId, CancellationToken ct)
        => await db.CuttingExecutions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && _activeStatuses.Contains(x.Status))
            .OrderByDescending(x => x.ScheduledAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
