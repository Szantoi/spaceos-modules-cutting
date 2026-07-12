using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

/// <summary>
/// Raised when a quote request is rejected.
/// </summary>
public record QuoteRejectedEvent(
    Guid QuoteId,
    string QuoteNumber,
    string Reason,
    Guid ReviewedByUserId,
    DateTime RejectedAt) : IDomainEvent;
