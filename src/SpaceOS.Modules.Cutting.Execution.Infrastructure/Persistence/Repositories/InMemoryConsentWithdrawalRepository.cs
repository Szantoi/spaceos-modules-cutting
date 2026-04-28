using System.Collections.Concurrent;
using SpaceOS.Modules.Cutting.Execution.Application.Entities;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IConsentWithdrawalRepository"/>.
/// Suitable for development and testing. Replace with a DB-backed implementation for production
/// when GDPR consent withdrawal volume warrants persistence across restarts.
/// </summary>
public sealed class InMemoryConsentWithdrawalRepository : IConsentWithdrawalRepository
{
    private readonly ConcurrentDictionary<Guid, ConsentWithdrawal> _store = new();

    /// <inheritdoc />
    public Task<ConsentWithdrawal?> PickupNextPendingAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var pending = _store.Values
            .FirstOrDefault(w => w.Status == ConsentWithdrawalStatus.Pending);

        return Task.FromResult<ConsentWithdrawal?>(pending);
    }

    /// <inheritdoc />
    public Task<ConsentWithdrawal?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _store.TryGetValue(id, out var withdrawal);
        return Task.FromResult<ConsentWithdrawal?>(withdrawal);
    }

    /// <inheritdoc />
    public Task<List<Guid>> ListAffectedExecutionsBatchAsync(
        Guid tenantId,
        Guid workerId,
        ConsentScope scope,
        int offset,
        int limit,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Stub: return empty — full implementation requires querying CuttingExecutions table
        return Task.FromResult(new List<Guid>());
    }

    /// <inheritdoc />
    public Task SaveAsync(ConsentWithdrawal withdrawal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(withdrawal);

        _store[withdrawal.Id] = withdrawal;
        return Task.CompletedTask;
    }
}
