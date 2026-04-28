using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Adapters.Events;

public sealed record AdapterHealthFailed(
    Guid TenantId,
    string AdapterName,
    string SanitizedErrorMessage,
    int ConsecutiveFailures,
    DateTimeOffset OccurredAt) : IDomainEvent;
