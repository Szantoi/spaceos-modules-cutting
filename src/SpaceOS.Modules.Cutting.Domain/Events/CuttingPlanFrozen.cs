using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

public sealed record CuttingPlanFrozen(
    Guid PlanId,
    Guid TenantId,
    DateTimeOffset FrozenAt
) : IDomainEvent;
