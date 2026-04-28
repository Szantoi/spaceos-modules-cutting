using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.StartExecution;

public sealed class StartExecutionCommandHandler(
    ICuttingExecutionRepository repository,
    IWorkerSecurityPolicy securityPolicy)
    : IRequestHandler<StartExecutionCommand, Result>
{
    public async Task<Result> Handle(StartExecutionCommand request, CancellationToken ct)
    {
        var execution = await repository.GetByIdAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result.NotFound($"Execution {request.ExecutionId} not found.");

        var hmacResult = WorkerEventHmac.Create(request.BadgeHmacBase64, request.HmacKeyVersion);
        if (!hmacResult.IsSuccess)
            return Result.Invalid(hmacResult.ValidationErrors);

        var result = execution.Start(request.WorkerId, hmacResult.Value, securityPolicy, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
