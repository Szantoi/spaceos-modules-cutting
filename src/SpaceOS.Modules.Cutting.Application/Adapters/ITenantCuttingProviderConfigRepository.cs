using SpaceOS.Modules.Cutting.Domain.Adapters;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>Repository for <see cref="TenantCuttingProviderConfig"/> aggregates.</summary>
public interface ITenantCuttingProviderConfigRepository
{
    /// <summary>Returns the config for the given tenant, or null if none exists.</summary>
    Task<TenantCuttingProviderConfig?> GetByTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Persists a new config.</summary>
    Task AddAsync(TenantCuttingProviderConfig config, CancellationToken ct);

    /// <summary>Persists changes to an existing config.</summary>
    Task UpdateAsync(TenantCuttingProviderConfig config, CancellationToken ct);
}
