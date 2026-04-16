using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

public sealed record CuttingSheetSubmittedEvent(
    Guid SheetId,
    Guid TenantId,
    string OrderReference,
    int LineCount) : IDomainEvent;
