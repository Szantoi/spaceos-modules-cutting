namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// EF entity for the <c>adapter_call_audit</c> partitioned table.
/// Not a domain aggregate — plain persistence class.
/// </summary>
public class AdapterCallAuditEntity
{
    public Guid CallId { get; set; }
    public Guid TenantId { get; set; }
    public string AdapterName { get; set; } = string.Empty;
    public string TransportName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? PayloadHash { get; set; }
    public int? PayloadSizeBytes { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public string Status { get; set; } = "started";
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
}
