using FluentValidation;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;

public sealed class ScheduleExecutionCommandValidator : AbstractValidator<ScheduleExecutionCommand>
{
    public ScheduleExecutionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SheetId).NotEmpty();
        RuleFor(x => x.WorkerId).NotEmpty();
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.MachineId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScheduleStart).NotEmpty();
        RuleFor(x => x.ScheduleEnd).NotEmpty().GreaterThan(x => x.ScheduleStart);
        RuleFor(x => x.TotalPanels).GreaterThan(0);
    }
}
