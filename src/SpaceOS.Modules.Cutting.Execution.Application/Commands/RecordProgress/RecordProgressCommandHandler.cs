using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;

public sealed class RecordProgressCommandHandler(
    ICuttingExecutionRepository repository,
    IWorkerSecurityPolicy securityPolicy)
    : IRequestHandler<RecordProgressCommand, Result>
{
    public async Task<Result> Handle(RecordProgressCommand request, CancellationToken ct)
    {
        var execution = await repository.GetByIdWithProgressAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result.NotFound($"Execution {request.ExecutionId} not found.");

        var hmacResult = WorkerEventHmac.Create(request.EventHmacBase64, request.HmacKeyVersion);
        if (!hmacResult.IsSuccess)
            return Result.Invalid(hmacResult.ValidationErrors);

        var result = execution.RecordProgress(
            request.EventId,
            request.Kind,
            request.Panel,
            request.OccurredAt,
            hmacResult.Value,
            securityPolicy,
            DateTime.UtcNow);

        if (!result.IsSuccess)
            return result;

        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
