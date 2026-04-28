using SpaceOS.Modules.Cutting.Domain.Adapters;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>Repository for <see cref="AdapterHealthRecord"/> aggregates.</summary>
public interface IAdapterHealthRecordRepository
{
    /// <summary>Returns the health record for the given tenant and adapter, or null.</summary>
    Task<AdapterHealthRecord?> GetAsync(Guid tenantId, string adapterName, CancellationToken ct);

    /// <summary>Persists a new health record.</summary>
    Task AddAsync(AdapterHealthRecord record, CancellationToken ct);

    /// <summary>Persists changes to an existing health record.</summary>
    Task UpdateAsync(AdapterHealthRecord record, CancellationToken ct);
}
