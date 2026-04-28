namespace SpaceOS.Modules.Cutting.Infrastructure.Outbox;

/// <summary>
/// Local transactional outbox message. Written atomically with aggregate changes so that
/// domain events can be dispatched reliably by a background processor.
/// </summary>
public sealed class LocalOutboxMessage
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant this event belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Optional batch group identifier (one batch per aggregate save).</summary>
    public Guid? BatchId { get; private set; }

    /// <summary>Sequence within the batch, preserving event order.</summary>
    public int? BatchSequenceNumber { get; private set; }

    /// <summary>Aggregate that emitted this event, when available.</summary>
    public Guid? AggregateId { get; private set; }

    /// <summary>CLR type name of the aggregate.</summary>
    public string? AggregateType { get; private set; }

    /// <summary>Simple type name of the domain event (e.g. "CuttingExecutionScheduled").</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>JSON-serialised event payload.</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>When the domain event occurred.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>When this message was successfully processed.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>Number of processing attempts made so far.</summary>
    public int Attempts { get; private set; }

    /// <summary>Last error message from a failed attempt.</summary>
    public string? LastError { get; private set; }

    /// <summary>Current processing status.</summary>
    public LocalOutboxStatus Status { get; private set; } = LocalOutboxStatus.Pending;

    private LocalOutboxMessage() { }

    /// <summary>
    /// Creates a new pending outbox message.
    /// </summary>
    public static LocalOutboxMessage Create(
        Guid tenantId,
        string eventType,
        string payloadJson,
        Guid? batchId = null,
        int? batchSeq = null,
        Guid? aggregateId = null,
        string? aggregateType = null)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(payloadJson);

        return new LocalOutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            PayloadJson = payloadJson,
            BatchId = batchId,
            BatchSequenceNumber = batchSeq,
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            OccurredAt = DateTimeOffset.UtcNow,
            Status = LocalOutboxStatus.Pending
        };
    }

    /// <summary>Marks the message as successfully processed.</summary>
    public void MarkProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
        Status = LocalOutboxStatus.Processed;
    }

    /// <summary>Marks the message as failed, incrementing the attempt counter.</summary>
    public void MarkFailed(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Attempts++;
        LastError = error;
        Status = LocalOutboxStatus.Failed;
    }
}
