using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.FreezeCuttingPlan;

public sealed record FreezeCuttingPlanCommand(Guid PlanId) : IRequest<Result<Unit>>;
