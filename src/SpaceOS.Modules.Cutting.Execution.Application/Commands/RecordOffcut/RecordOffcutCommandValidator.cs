using FluentValidation;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;

public sealed class RecordOffcutCommandValidator : AbstractValidator<RecordOffcutCommand>
{
    public RecordOffcutCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.MaterialId).NotEmpty();
        RuleFor(x => x.WidthMm).GreaterThan(0).LessThanOrEqualTo(5000);
        RuleFor(x => x.HeightMm).GreaterThan(0).LessThanOrEqualTo(5000);
    }
}
