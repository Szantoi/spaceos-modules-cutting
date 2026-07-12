using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Commands.CalculatePrice;

/// <summary>
/// Command to calculate price based on a PricingRule.
/// </summary>
public record CalculatePriceCommand : IRequest<Result<PriceCalculationResponseDto>>
{
    public Guid PricingRuleId { get; init; }
    public int Quantity { get; init; }
    public int LeadDays { get; init; }
    public Guid? MaterialId { get; init; }
}
