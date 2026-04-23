using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePriorityProfile;

public sealed class CreatePriorityProfileCommandHandler
    : IRequestHandler<CreatePriorityProfileCommand, Result<Guid>>
{
    private readonly IPriorityProfileRepository _repository;

    public CreatePriorityProfileCommandHandler(IPriorityProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreatePriorityProfileCommand request, CancellationToken ct)
    {
        PriorityProfile profile;
        try
        {
            profile = PriorityProfile.Create(
                tenantId: request.TenantId == Guid.Empty ? null : request.TenantId,
                name: request.Name,
                capacityModelId: request.CapacityModelId,
                reworkPolicyId: request.ReworkPolicyId,
                planningStrategyId: request.PlanningStrategyId,
                isDefault: request.IsDefault);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Invalid(new ValidationError(ex.Message));
        }

        await _repository.AddAsync(profile, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<Guid>.Success(profile.Id);
    }
}
