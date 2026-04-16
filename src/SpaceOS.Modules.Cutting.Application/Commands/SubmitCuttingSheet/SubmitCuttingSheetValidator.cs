using FluentValidation;

namespace SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;

public sealed class SubmitCuttingSheetValidator : AbstractValidator<SubmitCuttingSheetCommand>
{
    public SubmitCuttingSheetValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.OrderReference).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one cutting line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.PartName).NotEmpty();
            line.RuleFor(l => l.WidthMm).GreaterThan(0);
            line.RuleFor(l => l.HeightMm).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
