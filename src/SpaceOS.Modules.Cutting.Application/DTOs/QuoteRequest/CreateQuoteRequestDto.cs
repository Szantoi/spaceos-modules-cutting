namespace SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

/// <summary>
/// DTO for creating a new quote request.
/// </summary>
public sealed record CreateQuoteRequestDto
{
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required List<QuoteLineItemDto> Items { get; init; }
    public required string DeliveryAddress { get; init; }
    public DateTime? RequestedDeliveryDate { get; init; }
}

/// <summary>
/// DTO for a quote line item.
/// </summary>
public sealed record QuoteLineItemDto
{
    public required string MaterialType { get; init; }
    public required int WidthMm { get; init; }
    public required int HeightMm { get; init; }
    public required int Quantity { get; init; }
    public required string EdgingType { get; init; }
    public string? Notes { get; init; }
}
