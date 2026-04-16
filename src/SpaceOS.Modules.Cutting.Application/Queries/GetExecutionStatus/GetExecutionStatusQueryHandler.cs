using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;

public sealed class GetExecutionStatusQueryHandler : IRequestHandler<GetExecutionStatusQuery, Result<ExecutionStatusResponse>>
{
    private readonly ICuttingRepository _repository;

    public GetExecutionStatusQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ExecutionStatusResponse>> Handle(GetExecutionStatusQuery request, CancellationToken ct)
    {
        var execution = await _repository.GetExecutionBySheetIdAsync(request.SheetId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result<ExecutionStatusResponse>.NotFound($"No execution found for sheet {request.SheetId}.");

        return Result<ExecutionStatusResponse>.Success(new ExecutionStatusResponse(
            execution.CuttingSheetId,
            execution.Status.ToString(),
            execution.StartedAt,
            execution.CompletedAt,
            execution.WasteAreaCm2));
    }
}
