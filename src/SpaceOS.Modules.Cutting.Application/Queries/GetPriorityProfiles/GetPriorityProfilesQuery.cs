using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetPriorityProfiles;

public sealed record GetPriorityProfilesQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<PriorityProfileResponse>>>;

public sealed record PriorityProfileResponse(
    Guid Id,
    Guid? TenantId,
    string Name,
    bool IsDefault,
    string CapacityModelId,
    string ReworkPolicyId,
    string PlanningStrategyId);
