using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.PublishCuttingPlan;

public sealed class PublishCuttingPlanCommandHandler
    : IRequestHandler<PublishCuttingPlanCommand, Result<Unit>>
{
    private readonly ICuttingRepository _repository;

    public PublishCuttingPlanCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Unit>> Handle(PublishCuttingPlanCommand request, CancellationToken ct)
    {
        var plan = await _repository.GetCuttingPlanTrackedAsync(request.PlanId, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<Unit>.NotFound($"CuttingPlan {request.PlanId} not found.");

        var result = plan.Publish(request.ProfileSnapshotId);
        if (!result.IsSuccess)
            return Result<Unit>.Invalid(result.ValidationErrors.ToArray());

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Unit>.Success(Unit.Value);
    }
}
