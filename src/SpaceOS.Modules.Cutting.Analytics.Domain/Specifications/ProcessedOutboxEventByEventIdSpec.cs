using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;

/// <summary>Checks the dedup ledger for a previously processed (eventId, subscriberName) pair.</summary>
public sealed class ProcessedOutboxEventByEventIdSpec : Specification<ProcessedOutboxEvent>
{
    /// <param name="eventId">Original outbox event identifier.</param>
    /// <param name="subscriberName">Name of the projector checking deduplication.</param>
    public ProcessedOutboxEventByEventIdSpec(Guid eventId, string subscriberName)
    {
        Query.Where(e => e.EventId == eventId && e.SubscriberName == subscriberName);
    }
}
