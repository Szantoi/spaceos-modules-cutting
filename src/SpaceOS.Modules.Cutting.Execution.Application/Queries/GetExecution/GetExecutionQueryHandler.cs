using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetExecution;

public sealed class GetExecutionQueryHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<GetExecutionQuery, Result<ExecutionDto>>
{
    public async Task<Result<ExecutionDto>> Handle(GetExecutionQuery request, CancellationToken ct)
    {
        var execution = await repository.GetByIdAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result<ExecutionDto>.NotFound($"Execution {request.ExecutionId} not found.");

        return Result<ExecutionDto>.Success(new ExecutionDto(
            execution.Id,
            execution.TenantId,
            execution.SheetId,
            execution.Status.ToString(),
            execution.PanelsCompleted,
            execution.TotalPanels,
            execution.StartedAt,
            execution.CompletedAt));
    }
}
