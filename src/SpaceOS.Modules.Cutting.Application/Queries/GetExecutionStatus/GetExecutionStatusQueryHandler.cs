using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;

/// <summary>
/// Returns execution status for a cutting sheet. Phase 3 stub — superseded by Phase 4
/// <c>GetExecutionQuery</c> in <c>SpaceOS.Modules.Cutting.Execution.Application</c>.
/// </summary>
public sealed class GetExecutionStatusQueryHandler : IRequestHandler<GetExecutionStatusQuery, Result<ExecutionStatusResponse>>
{
    // Repository kept for future Phase 4 integration
    private readonly ICuttingRepository _repository;

    public GetExecutionStatusQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<ExecutionStatusResponse>> Handle(GetExecutionStatusQuery request, CancellationToken ct)
    {
        // Phase 3 stub — query Phase 4 ICuttingExecutionRepository for real data.
        return Task.FromResult(Result<ExecutionStatusResponse>.NotFound($"No execution found for sheet {request.SheetId}."));
    }
}
