using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

/// <summary>
/// Raised when a quote request is approved with a price.
/// </summary>
public record QuoteApprovedEvent(
    Guid QuoteId,
    string QuoteNumber,
    decimal QuotedPriceAmount,
    string QuotedPriceCurrency,
    Guid ReviewedByUserId,
    DateTime ReviewedAt) : IDomainEvent;
