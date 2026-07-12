using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetPricingRule;

/// <summary>
/// Query to get a PricingRule by its ID.
/// </summary>
public record GetPricingRuleQuery : IRequest<Result<PricingRuleDto>>
{
    public Guid PricingRuleId { get; init; }
}
