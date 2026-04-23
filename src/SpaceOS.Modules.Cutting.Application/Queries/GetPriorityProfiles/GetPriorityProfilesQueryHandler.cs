using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetPriorityProfiles;

public sealed class GetPriorityProfilesQueryHandler
    : IRequestHandler<GetPriorityProfilesQuery, Result<IReadOnlyList<PriorityProfileResponse>>>
{
    private readonly IPriorityProfileRepository _repository;

    public GetPriorityProfilesQueryHandler(IPriorityProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<PriorityProfileResponse>>> Handle(
        GetPriorityProfilesQuery request,
        CancellationToken ct)
    {
        var profiles = await _repository
            .GetByTenantAsync(request.TenantId, ct)
            .ConfigureAwait(false);

        var response = profiles
            .Select(p => new PriorityProfileResponse(
                p.Id,
                p.TenantId,
                p.Name,
                p.IsDefault,
                p.CapacityModelId,
                p.ReworkPolicyId,
                p.PlanningStrategyId))
            .ToList();

        return Result<IReadOnlyList<PriorityProfileResponse>>.Success(response);
    }
}
