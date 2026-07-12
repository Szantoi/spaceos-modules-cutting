namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>
/// DTO for PricingRule responses (GET endpoints).
/// </summary>
public record PricingRuleDto
{
    public Guid Id { get; init; }
    public Guid SupplierId { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public decimal BasePricePerUnit { get; init; }
    public string Status { get; init; } = string.Empty; // "Draft" | "Active" | "Archived"
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }

    public List<QuantityBreakpointDto> QuantityBreakpoints { get; init; } = new();
    public List<LeadTimeAdjustmentDto> LeadTimeAdjustments { get; init; } = new();
    public List<MaterialSurchargeDto> MaterialSurcharges { get; init; } = new();
}

public record QuantityBreakpointDto
{
    public Guid Id { get; init; }
    public int MinQuantity { get; init; }
    public int MaxQuantity { get; init; }
    public decimal DiscountPercent { get; init; }
}

public record LeadTimeAdjustmentDto
{
    public Guid Id { get; init; }
    public int LeadDays { get; init; }
    public decimal AdjustmentFactor { get; init; }
}

public record MaterialSurchargeDto
{
    public Guid Id { get; init; }
    public Guid MaterialId { get; init; }
    public decimal SurchargePercent { get; init; }
}
