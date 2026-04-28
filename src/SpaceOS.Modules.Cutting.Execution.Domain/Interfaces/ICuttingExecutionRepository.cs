using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

/// <summary>Repository port for the CuttingExecution aggregate.</summary>
public interface ICuttingExecutionRepository
{
    Task<CuttingExecution?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CuttingExecution?> GetByIdWithProgressAsync(Guid id, CancellationToken ct);
    Task AddAsync(CuttingExecution execution, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
