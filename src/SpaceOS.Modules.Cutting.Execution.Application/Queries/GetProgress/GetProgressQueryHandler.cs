using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetProgress;

public sealed class GetProgressQueryHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<GetProgressQuery, Result<IReadOnlyList<ProgressEventDto>>>
{
    public async Task<Result<IReadOnlyList<ProgressEventDto>>> Handle(GetProgressQuery request, CancellationToken ct)
    {
        var execution = await repository.GetByIdWithProgressAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result<IReadOnlyList<ProgressEventDto>>.NotFound($"Execution {request.ExecutionId} not found.");

        var dtos = execution.ProgressEvents
            .Select(e => new ProgressEventDto(e.EventId, e.Kind.ToString(), e.Panel, e.OccurredAt))
            .ToList();

        return Result<IReadOnlyList<ProgressEventDto>>.Success(dtos);
    }
}
