using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.UpdateCuttingPlanStatus;

public sealed record UpdateCuttingPlanStatusCommand(Guid PlanId, string NewStatus) : IRequest<Result<Unit>>;
