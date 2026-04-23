using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.EventHandlers;
using SpaceOS.Modules.Cutting.Domain.Events;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.FreezeCuttingPlan;

public sealed class FreezeCuttingPlanCommandHandler
    : IRequestHandler<FreezeCuttingPlanCommand, Result<Unit>>
{
    private readonly ICuttingRepository _repository;
    private readonly IMediator _mediator;

    public FreezeCuttingPlanCommandHandler(ICuttingRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Result<Unit>> Handle(FreezeCuttingPlanCommand request, CancellationToken ct)
    {
        var plan = await _repository.GetCuttingPlanTrackedAsync(request.PlanId, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<Unit>.NotFound($"CuttingPlan {request.PlanId} not found.");

        var result = plan.Freeze();
        if (!result.IsSuccess)
            return Result<Unit>.Invalid(result.ValidationErrors.ToArray());

        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        // Dispatch domain events as MediatR notifications (best-effort)
        foreach (var domainEvent in plan.PopDomainEvents())
        {
            if (domainEvent is CuttingPlanFrozen frozenEvent)
            {
                await _mediator.Publish(
                    new CuttingPlanFrozenNotification(frozenEvent.PlanId, frozenEvent.TenantId, frozenEvent.FrozenAt),
                    ct).ConfigureAwait(false);
            }
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
