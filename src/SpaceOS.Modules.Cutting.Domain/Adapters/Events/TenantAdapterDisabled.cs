using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Adapters.Events;

public sealed record TenantAdapterDisabled(
    Guid TenantId,
    Guid ActorId,
    DateTimeOffset OccurredAt) : IDomainEvent;
