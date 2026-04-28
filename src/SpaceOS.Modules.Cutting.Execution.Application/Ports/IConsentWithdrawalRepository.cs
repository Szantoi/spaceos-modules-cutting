using SpaceOS.Modules.Cutting.Execution.Application.Entities;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Ports;

/// <summary>Repository port for ConsentWithdrawal processing queue.</summary>
public interface IConsentWithdrawalRepository
{
    Task<ConsentWithdrawal?> PickupNextPendingAsync(CancellationToken ct);
    Task<ConsentWithdrawal?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Guid>> ListAffectedExecutionsBatchAsync(Guid tenantId, Guid workerId, ConsentScope scope, int offset, int limit, CancellationToken ct);
    Task SaveAsync(ConsentWithdrawal withdrawal, CancellationToken ct);
}
