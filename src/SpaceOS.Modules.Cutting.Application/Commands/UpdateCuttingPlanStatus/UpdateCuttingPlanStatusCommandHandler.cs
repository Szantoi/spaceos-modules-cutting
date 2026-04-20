using Ardalis.Result;
using MediatR;
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

        try
        {
            plan.UpdateStatus(request.NewStatus);
        }
        catch (ArgumentException ex)
        {
            return Result<Unit>.Invalid(new ValidationError(ex.Message));
        }

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Unit>.Success(Unit.Value);
    }
}
