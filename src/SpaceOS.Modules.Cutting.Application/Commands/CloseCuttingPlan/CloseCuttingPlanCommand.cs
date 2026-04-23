using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.CloseCuttingPlan;

public sealed record CloseCuttingPlanCommand(Guid PlanId) : IRequest<Result<Unit>>;
