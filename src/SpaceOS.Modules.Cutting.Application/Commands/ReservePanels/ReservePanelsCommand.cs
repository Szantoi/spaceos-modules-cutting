using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.ReservePanels;

public sealed record ReservePanelsCommand(Guid PlanId, Guid TenantId) : IRequest<Result<int>>;
