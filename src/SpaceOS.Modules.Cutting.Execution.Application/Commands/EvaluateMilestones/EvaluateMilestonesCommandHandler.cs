using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Services;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.EvaluateMilestones;

public sealed class EvaluateMilestonesCommandHandler(
    ICuttingExecutionRepository repository,
    PredicateFactoryV1 predicateFactory)
    : IRequestHandler<EvaluateMilestonesCommand, Result>
{
    public async Task<Result> Handle(EvaluateMilestonesCommand request, CancellationToken ct)
    {
        var execution = await repository.GetByIdWithProgressAsync(request.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null)
            return Result.NotFound($"Execution {request.ExecutionId} not found.");

        var predicates = execution.Milestones
            .Select(m => predicateFactory.Create(m.Kind, m.ConfigJson, m.ConfigVersion));

        var result = execution.EvaluateMilestones(predicates, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        await repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
