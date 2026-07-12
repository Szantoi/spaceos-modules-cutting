using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

/// <summary>
/// Raised when a public quote request is submitted.
/// </summary>
public record QuoteRequestSubmittedEvent(
    Guid QuoteId,
    string QuoteNumber,
    string CustomerEmail,
    string CustomerName,
    int ItemCount,
    DateTime SubmittedAt) : IDomainEvent;
