using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

/// <summary>
/// Raised when a quote is accepted by the customer and converted to an order.
/// </summary>
public record QuoteConvertedToOrderEvent(
    Guid QuoteId,
    string QuoteNumber,
    Guid CuttingSheetId,
    DateTime ConvertedAt) : IDomainEvent;
