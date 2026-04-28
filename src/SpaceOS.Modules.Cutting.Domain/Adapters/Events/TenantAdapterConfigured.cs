using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Adapters.Events;

public sealed record TenantAdapterConfigured(
    Guid TenantId,
    string AdapterName,
    string TransportName,
    Guid ActorId,
    DateTimeOffset OccurredAt) : IDomainEvent;
