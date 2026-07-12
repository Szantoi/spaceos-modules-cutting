using FluentValidation;

namespace SpaceOS.Modules.Cutting.Application.Commands.ActivatePricingRule;

/// <summary>
/// Validator for ActivatePricingRuleCommand.
/// </summary>
public class ActivatePricingRuleCommandValidator : AbstractValidator<ActivatePricingRuleCommand>
{
    public ActivatePricingRuleCommandValidator()
    {
        RuleFor(x => x.PricingRuleId)
            .NotEmpty()
            .WithMessage("PricingRuleId is required.");
    }
}
