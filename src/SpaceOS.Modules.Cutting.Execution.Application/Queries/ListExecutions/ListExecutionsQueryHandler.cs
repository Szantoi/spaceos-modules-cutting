using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.Specifications;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.ListExecutions;

public sealed class ListExecutionsQueryHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<ListExecutionsQuery, Result<IReadOnlyList<ExecutionSummaryDto>>>
{
    private readonly ICuttingExecutionRepository _repository = repository;

    public async Task<Result<IReadOnlyList<ExecutionSummaryDto>>> Handle(ListExecutionsQuery request, CancellationToken ct)
    {
        // ActiveExecutionsByTenantSpec is evaluated by the infrastructure layer implementation.
        // This handler returns an empty list — a list-all method will be added to the repository in Phase 4B.
        _ = _repository;
        _ = new ActiveExecutionsByTenantSpec(request.TenantId);
        await Task.CompletedTask.ConfigureAwait(false);
        return Result<IReadOnlyList<ExecutionSummaryDto>>.Success(Array.Empty<ExecutionSummaryDto>());
    }
}
