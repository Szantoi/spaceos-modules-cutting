using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.CancelExecution;

public sealed class CancelExecutionCommandHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<CancelExecutionCommand, Result>
{
    public async Task<Result> Handle(CancelExecutionCommand request, CancellationToken ct)
    {
        var execution = await repository.GetByIdAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result.NotFound($"Execution {request.ExecutionId} not found.");

        var result = execution.Cancel(request.Reason, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
