using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Entities;

/// <summary>Owned entity recording a single worker progress event during execution.</summary>
public sealed class ProgressEvent
{
    public Guid EventId { get; private set; }
    public ProgressEventKind Kind { get; private set; }
    public int? Panel { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventHmac { get; private set; } = string.Empty;

    private ProgressEvent() { }

    internal static ProgressEvent Create(Guid eventId, ProgressEventKind kind, int? panel, DateTime occurredAt, string eventHmac)
        => new()
        {
            EventId = eventId,
            Kind = kind,
            Panel = panel,
            OccurredAt = occurredAt,
            EventHmac = eventHmac
        };
}
