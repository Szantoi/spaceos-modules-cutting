using FluentValidation;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.CancelExecution;

public sealed class CancelExecutionCommandValidator : AbstractValidator<CancelExecutionCommand>
{
    public CancelExecutionCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();
    }
}
