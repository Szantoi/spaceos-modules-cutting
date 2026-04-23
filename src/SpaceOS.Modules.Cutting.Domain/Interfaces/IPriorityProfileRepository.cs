using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface IPriorityProfileRepository
{
    Task AddAsync(PriorityProfile profile, CancellationToken ct = default);
    Task<PriorityProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PriorityProfile>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<PriorityProfile?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<PriorityProfile>> GetGlobalPresetsAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
