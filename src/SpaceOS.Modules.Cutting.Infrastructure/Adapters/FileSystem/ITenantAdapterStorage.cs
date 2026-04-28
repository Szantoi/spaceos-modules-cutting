namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.FileSystem;

/// <summary>
/// SEC-01: Tenant-scoped file storage for adapter file-exchange workflows.
/// All paths are canonicalized and validated for containment before access.
/// </summary>
public interface ITenantAdapterStorage
{
    /// <summary>Writes <paramref name="content"/> to the tenant inbox for later pickup by the external system.</summary>
    Task WriteToInboxAsync(Guid tenantId, string adapterName, string correlationId, byte[] content, CancellationToken ct);

    /// <summary>
    /// Attempts to read the result from the tenant outbox.
    /// Returns null when no <c>.complete</c> file exists for the given correlationId.
    /// </summary>
    Task<byte[]?> TryReadFromOutboxAsync(Guid tenantId, string adapterName, string correlationId, CancellationToken ct);

    /// <summary>Returns the root directory for the given tenant's adapter data.</summary>
    string GetTenantRoot(Guid tenantId, string adapterName);

    /// <summary>Returns the outbox path for the given tenant and adapter.</summary>
    string GetOutboxPath(Guid tenantId, string adapterName);

    /// <summary>Checks whether the tenant root directory exists and is accessible.</summary>
    Task<bool> CheckTenantRootAccessibleAsync(Guid tenantId, string adapterName, CancellationToken ct);
}
