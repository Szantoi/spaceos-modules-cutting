namespace SpaceOS.Modules.Cutting.Analytics.Domain.Common;

/// <summary>
/// Deduplication ledger entry. One row per (EventId, SubscriberName) pair.
/// Inserted before applying a projection so that retries are idempotent.
/// </summary>
public sealed class ProcessedOutboxEvent
{
    /// <summary>Original outbox event identifier.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Name of the projector that consumed this event.</summary>
    public string SubscriberName { get; private set; } = string.Empty;

    /// <summary>Tenant the event belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>UTC time this dedup record was inserted.</summary>
    public DateTime CreatedAt { get; private set; }

    private ProcessedOutboxEvent() { }

    /// <summary>Creates a new deduplication record.</summary>
    public static ProcessedOutboxEvent Create(Guid eventId, string subscriberName, Guid tenantId)
        => new()
        {
            EventId = eventId,
            SubscriberName = subscriberName,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };
}
