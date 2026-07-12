namespace SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

/// <summary>
/// Response DTO after creating a quote request.
/// </summary>
public sealed record QuoteRequestResponseDto
{
    public required string QuoteId { get; init; }
    public required string QuoteNumber { get; init; }
    public required string TrackingToken { get; init; }
    public required string Status { get; init; }
    public required string EstimatedResponseTime { get; init; }
    public required string TrackingUrl { get; init; }
}

/// <summary>
/// Response DTO for tracking a quote.
/// </summary>
public sealed record QuoteTrackingDto
{
    public required string QuoteNumber { get; init; }
    public required string Status { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public QuotedPriceDto? QuotedPrice { get; init; }
    public DateTime? EstimatedDelivery { get; init; }
    public QuoteActionDto? ActionRequired { get; init; }
}

public sealed record QuotedPriceDto
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
}

public sealed record QuoteActionDto
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required string AcceptUrl { get; init; }
}

/// <summary>
/// Response DTO for listing quote requests (admin view).
/// </summary>
public sealed record QuoteRequestListItemDto
{
    public required Guid Id { get; init; }
    public required string QuoteNumber { get; init; }
    public required string Status { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public required int ItemCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public QuotedPriceDto? QuotedPrice { get; init; }
}
