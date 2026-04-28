using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;

public sealed record ScheduleExecutionCommand(
    Guid TenantId,
    Guid SheetId,
    Guid WorkerId,
    Guid EnrollmentId,
    string MachineId,
    DateTime ScheduleStart,
    DateTime ScheduleEnd,
    int TotalPanels) : IRequest<Result<Guid>>;
