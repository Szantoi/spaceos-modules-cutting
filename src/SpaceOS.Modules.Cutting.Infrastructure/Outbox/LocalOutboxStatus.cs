namespace SpaceOS.Modules.Cutting.Infrastructure.Outbox;

/// <summary>Processing state of a local outbox message.</summary>
public enum LocalOutboxStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3
}
