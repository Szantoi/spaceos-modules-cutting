using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.UpdateCuttingPlanStatus;

public sealed class UpdateCuttingPlanStatusCommandHandler : IRequestHandler<UpdateCuttingPlanStatusCommand, Result<Unit>>
{
    private readonly ICuttingRepository _repository;

    public UpdateCuttingPlanStatusCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Unit>> Handle(UpdateCuttingPlanStatusCommand request, CancellationToken ct)
    {
        var plan = await _repository.GetCuttingPlanTrackedAsync(request.PlanId, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<Unit>.NotFound($"CuttingPlan {request.PlanId} not found.");

        var status = ParseStatus(request.NewStatus);
        if (status is null)
            return Result<Unit>.Invalid(new ValidationError($"Invalid status '{request.NewStatus}'. Valid values: Draft, Published, Frozen, Closed."));

#pragma warning disable CS0618
        plan.UpdateStatus(status.Value);
#pragma warning restore CS0618

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Unit>.Success(Unit.Value);
    }

    private static CuttingPlanStatus? ParseStatus(string raw) =>
        raw.Trim().ToLowerInvariant() switch
        {
            "draft"      => CuttingPlanStatus.Draft,
            "approved"   => CuttingPlanStatus.Published,  // backwards-compat alias
            "published"  => CuttingPlanStatus.Published,
            "inprogress" => CuttingPlanStatus.Frozen,      // backwards-compat alias
            "frozen"     => CuttingPlanStatus.Frozen,
            "closed"     => CuttingPlanStatus.Closed,
            _            => null
        };
}
