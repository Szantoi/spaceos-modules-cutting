using FluentValidation;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePricingRule;

/// <summary>
/// Validator for CreatePricingRuleCommand.
/// </summary>
public class CreatePricingRuleCommandValidator : AbstractValidator<CreatePricingRuleCommand>
{
    public CreatePricingRuleCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("SupplierId is required.");

        RuleFor(x => x.ProductCategory)
            .NotEmpty()
            .WithMessage("ProductCategory is required.")
            .MaximumLength(100)
            .WithMessage("ProductCategory must not exceed 100 characters.");

        RuleFor(x => x.BasePricePerUnit)
            .GreaterThan(0)
            .WithMessage("BasePricePerUnit must be greater than zero.");

        RuleFor(x => x.QuantityBreakpoints)
            .NotEmpty()
            .WithMessage("At least one QuantityBreakpoint is required.");

        RuleForEach(x => x.QuantityBreakpoints).ChildRules(breakpoint =>
        {
            breakpoint.RuleFor(b => b.MinQuantity)
                .GreaterThanOrEqualTo(1)
                .WithMessage("MinQuantity must be at least 1.");

            breakpoint.RuleFor(b => b.MaxQuantity)
                .GreaterThanOrEqualTo(1)
                .WithMessage("MaxQuantity must be at least 1.")
                .GreaterThanOrEqualTo(b => b.MinQuantity)
                .WithMessage("MaxQuantity must be greater than or equal to MinQuantity.");

            breakpoint.RuleFor(b => b.DiscountPercent)
                .GreaterThanOrEqualTo(0)
                .WithMessage("DiscountPercent must be non-negative.")
                .LessThanOrEqualTo(100)
                .WithMessage("DiscountPercent cannot exceed 100%.");
        });

        RuleForEach(x => x.LeadTimeAdjustments).ChildRules(adjustment =>
        {
            adjustment.RuleFor(a => a.LeadDays)
                .GreaterThanOrEqualTo(0)
                .WithMessage("LeadDays must be non-negative.");

            adjustment.RuleFor(a => a.AdjustmentFactor)
                .GreaterThanOrEqualTo(0)
                .WithMessage("AdjustmentFactor must be non-negative.");
        });

        RuleForEach(x => x.MaterialSurcharges).ChildRules(surcharge =>
        {
            surcharge.RuleFor(s => s.MaterialId)
                .NotEmpty()
                .WithMessage("MaterialId is required.");

            surcharge.RuleFor(s => s.SurchargePercent)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SurchargePercent must be non-negative.");
        });
    }
}
