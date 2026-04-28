namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>
/// Writes adapter call audit entries. Implementations must sanitize all error fields before storage.
/// </summary>
public interface IAdapterCallAuditWriter
{
    /// <summary>Records the start of an adapter call.</summary>
    Task RecordSubmitStartedAsync(Guid callId, string adapterName, string methodName, Guid tenantId, CancellationToken ct);

    /// <summary>Records successful completion of an adapter call.</summary>
    Task RecordSubmitCompletedAsync(Guid callId, string correlationId, CancellationToken ct);

    /// <summary>Records a validation or domain failure for an adapter call.</summary>
    Task RecordFailureAsync(Guid callId, IEnumerable<string> errors, CancellationToken ct);

    /// <summary>Records an unhandled exception during an adapter call.</summary>
    Task RecordExceptionAsync(Guid callId, Exception ex, CancellationToken ct);

    /// <summary>Records that capability resolution fell back to the builtin adapter.</summary>
    Task RecordCapabilityFallbackAsync(string providerName, string capability, Guid tenantId, CancellationToken ct);
}
