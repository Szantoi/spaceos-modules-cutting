using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Events;

public sealed record WasteRecordedEvent(
    Guid ExecutionId,
    Guid TenantId,
    Guid CuttingSheetId,
    decimal WasteAreaCm2) : IDomainEvent;
