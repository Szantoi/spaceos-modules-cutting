using FluentValidation;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.EvaluateMilestones;

public sealed class EvaluateMilestonesCommandValidator : AbstractValidator<EvaluateMilestonesCommand>
{
    public EvaluateMilestonesCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
