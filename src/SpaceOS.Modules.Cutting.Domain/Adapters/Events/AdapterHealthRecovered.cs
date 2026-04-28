using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Adapters.Events;

public sealed record AdapterHealthRecovered(
    Guid TenantId,
    string AdapterName,
    DateTimeOffset OccurredAt) : IDomainEvent;
