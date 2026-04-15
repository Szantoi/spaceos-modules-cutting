namespace SpaceOS.Modules.Cutting.Contracts.Events;

/// <summary>Raised when cutting machine execution of a sheet finishes (successfully or with error).</summary>
public sealed record CuttingExecutionCompletedEvent(
    Guid TenantId,
    Guid SheetId,
    bool Success,
    decimal WastePercentage,
    DateTime OccurredAt);
