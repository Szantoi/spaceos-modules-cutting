namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>
/// Checks (and records) whether an outbox event has already been processed by a given subscriber.
/// Implementations must atomically insert the dedup record or return true if it already exists.
/// </summary>
public interface IProjectionIdempotencyGate
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="eventId"/> was already processed
    /// by <paramref name="subscriberName"/> for <paramref name="tenantId"/>.
    /// </summary>
    Task<bool> IsAlreadyProcessedAsync(
        Guid eventId, string subscriberName, Guid tenantId, CancellationToken ct);
}
