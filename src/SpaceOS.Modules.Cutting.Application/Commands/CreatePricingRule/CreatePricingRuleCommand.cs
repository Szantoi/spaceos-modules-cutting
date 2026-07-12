using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePricingRule;

/// <summary>
/// Command to create a new PricingRule in draft state.
/// </summary>
public record CreatePricingRuleCommand : IRequest<Result<PricingRuleDto>>
{
    public Guid SupplierId { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public decimal BasePricePerUnit { get; init; }

    public List<CreateQuantityBreakpointDto> QuantityBreakpoints { get; init; } = new();
    public List<CreateLeadTimeAdjustmentDto> LeadTimeAdjustments { get; init; } = new();
    public List<CreateMaterialSurchargeDto> MaterialSurcharges { get; init; } = new();
}
