using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetMilestones;

public sealed class GetMilestonesQueryHandler(ICuttingExecutionRepository repository)
    : IRequestHandler<GetMilestonesQuery, Result<IReadOnlyList<MilestoneDto>>>
{
    public async Task<Result<IReadOnlyList<MilestoneDto>>> Handle(GetMilestonesQuery request, CancellationToken ct)
    {
        var execution = await repository.GetByIdAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result<IReadOnlyList<MilestoneDto>>.NotFound($"Execution {request.ExecutionId} not found.");

        var dtos = execution.Milestones
            .Select(m => new MilestoneDto(m.MilestoneId, m.Kind.ToString(), m.Status.ToString(), m.ReachedAt))
            .ToList();

        return Result<IReadOnlyList<MilestoneDto>>.Success(dtos);
    }
}
