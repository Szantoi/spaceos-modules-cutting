namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>
/// Request DTO for calculating price based on a PricingRule.
/// </summary>
public record CalculatePriceRequestDto
{
    public int Quantity { get; init; }
    public int LeadDays { get; init; }
    public Guid? MaterialId { get; init; }
}

/// <summary>
/// Response DTO for price calculation.
/// </summary>
public record PriceCalculationResponseDto
{
    public decimal Price { get; init; }
    public string Breakdown { get; init; } = string.Empty;
}
