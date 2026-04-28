using FluentValidation;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.StartExecution;

public sealed class StartExecutionCommandValidator : AbstractValidator<StartExecutionCommand>
{
    public StartExecutionCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.WorkerId).NotEmpty();
        RuleFor(x => x.BadgeHmacBase64).NotEmpty().MaximumLength(512);
        RuleFor(x => x.HmacKeyVersion).NotEmpty().MaximumLength(50);
    }
}
