namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>
/// DTO for creating a new PricingRule.
/// </summary>
public record CreatePricingRuleDto
{
    public Guid SupplierId { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public decimal BasePricePerUnit { get; init; }

    public List<CreateQuantityBreakpointDto> QuantityBreakpoints { get; init; } = new();
    public List<CreateLeadTimeAdjustmentDto> LeadTimeAdjustments { get; init; } = new();
    public List<CreateMaterialSurchargeDto> MaterialSurcharges { get; init; } = new();
}

public record CreateQuantityBreakpointDto
{
    public int MinQuantity { get; init; }
    public int MaxQuantity { get; init; }
    public decimal DiscountPercent { get; init; }
}

public record CreateLeadTimeAdjustmentDto
{
    public int LeadDays { get; init; }
    public decimal AdjustmentFactor { get; init; }
}

public record CreateMaterialSurchargeDto
{
    public Guid MaterialId { get; init; }
    public decimal SurchargePercent { get; init; }
}
