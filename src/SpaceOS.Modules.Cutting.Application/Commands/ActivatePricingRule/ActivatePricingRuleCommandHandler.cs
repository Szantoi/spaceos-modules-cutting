using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.ActivatePricingRule;

/// <summary>
/// Handler for ActivatePricingRuleCommand.
/// Transitions a PricingRule from Draft to Active state.
/// </summary>
public class ActivatePricingRuleCommandHandler : IRequestHandler<ActivatePricingRuleCommand, Result>
{
    private readonly ICuttingRepository _repository;

    public ActivatePricingRuleCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ActivatePricingRuleCommand request, CancellationToken ct)
    {
        try
        {
            // Fetch the PricingRule aggregate (tracked for updates)
            var pricingRule = await _repository.GetPricingRuleByIdAsync(request.PricingRuleId, ct).ConfigureAwait(false);

            if (pricingRule == null)
            {
                return Result.NotFound($"PricingRule with ID {request.PricingRuleId} not found.");
            }

            // Activate the rule (FSM transition)
            pricingRule.Activate();

            // Persist changes
            await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Error(ex.Message);
        }
    }
}
