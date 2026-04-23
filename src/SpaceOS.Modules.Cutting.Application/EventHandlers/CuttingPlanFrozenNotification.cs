using MediatR;

namespace SpaceOS.Modules.Cutting.Application.EventHandlers;

/// <summary>
/// MediatR notification published after a CuttingPlan is successfully frozen.
/// Translated from the CuttingPlanFrozen domain event by FreezeCuttingPlanCommandHandler.
/// </summary>
public sealed record CuttingPlanFrozenNotification(
    Guid PlanId,
    Guid TenantId,
    DateTimeOffset FrozenAt
) : INotification;
