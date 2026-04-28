using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;

public sealed class ScheduleExecutionCommandHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<ScheduleExecutionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ScheduleExecutionCommand request, CancellationToken ct)
    {
        var workerResult = WorkerAssignment.Create(request.WorkerId, request.EnrollmentId);
        if (!workerResult.IsSuccess)
            return Result<Guid>.Invalid(workerResult.ValidationErrors);

        var windowResult = ScheduleWindow.Create(request.ScheduleStart, request.ScheduleEnd);
        if (!windowResult.IsSuccess)
            return Result<Guid>.Invalid(windowResult.ValidationErrors);

        var executionResult = CuttingExecution.Schedule(
            request.SheetId,
            workerResult.Value,
            request.MachineId,
            windowResult.Value,
            request.TotalPanels,
            request.TenantId);

        if (!executionResult.IsSuccess)
            return Result<Guid>.Invalid(executionResult.ValidationErrors);

        await repository.AddAsync(executionResult.Value, ct).ConfigureAwait(false);
        await repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<Guid>.Success(executionResult.Value.Id);
    }
}
