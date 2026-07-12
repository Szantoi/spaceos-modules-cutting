using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.ActivatePricingRule;

/// <summary>
/// Command to activate a PricingRule (transition from Draft to Active).
/// </summary>
public record ActivatePricingRuleCommand : IRequest<Result>
{
    public Guid PricingRuleId { get; init; }
}
