using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;

public sealed class RecordOffcutCommandHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<RecordOffcutCommand, Result>
{
    public async Task<Result> Handle(RecordOffcutCommand request, CancellationToken ct)
    {
        var execution = await repository.GetByIdAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result.NotFound($"Execution {request.ExecutionId} not found.");

        var offcutResult = OffcutEvent.Create(request.MaterialId, request.WidthMm, request.HeightMm);
        if (!offcutResult.IsSuccess)
            return Result.Invalid(offcutResult.ValidationErrors);

        var result = execution.RecordOffcut(offcutResult.Value, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
