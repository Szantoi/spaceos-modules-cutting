using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreatePriorityProfile;

public sealed record CreatePriorityProfileCommand(
    Guid TenantId,
    string Name,
    string CapacityModelId,
    string ReworkPolicyId,
    string PlanningStrategyId,
    bool IsDefault = false) : IRequest<Result<Guid>>;
