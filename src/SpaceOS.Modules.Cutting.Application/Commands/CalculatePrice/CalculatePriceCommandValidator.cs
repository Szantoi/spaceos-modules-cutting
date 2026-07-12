using FluentValidation;

namespace SpaceOS.Modules.Cutting.Application.Commands.CalculatePrice;

/// <summary>
/// Validator for CalculatePriceCommand.
/// </summary>
public class CalculatePriceCommandValidator : AbstractValidator<CalculatePriceCommand>
{
    public CalculatePriceCommandValidator()
    {
        RuleFor(x => x.PricingRuleId)
            .NotEmpty()
            .WithMessage("PricingRuleId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Quantity must be at least 1.");

        RuleFor(x => x.LeadDays)
            .GreaterThanOrEqualTo(0)
            .WithMessage("LeadDays must be non-negative.");
    }
}
