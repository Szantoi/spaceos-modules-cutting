using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

/// <summary>
/// Raised when a public quote request is created (Q3 Track A - MSG-BACKEND-030 Phase 4).
/// This event can be used to trigger email notifications, analytics, etc.
/// </summary>
public record PublicQuoteRequestCreatedEvent(
    Guid QuoteId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string Material,
    int Quantity,
    string Urgency,
    DateTime CreatedAt) : IDomainEvent;
