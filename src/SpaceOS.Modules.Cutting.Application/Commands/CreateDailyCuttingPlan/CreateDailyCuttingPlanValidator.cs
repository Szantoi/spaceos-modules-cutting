using FluentValidation;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateDailyCuttingPlan;

public sealed class CreateDailyCuttingPlanValidator : AbstractValidator<CreateDailyCuttingPlanCommand>
{
    public CreateDailyCuttingPlanValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PlanDate).NotEqual(default(DateTime));
        RuleFor(x => x.Batches).NotEmpty();
    }
}
