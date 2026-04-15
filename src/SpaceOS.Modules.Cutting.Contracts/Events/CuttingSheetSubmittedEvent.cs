namespace SpaceOS.Modules.Cutting.Contracts.Events;

/// <summary>Raised when a cutting sheet is submitted to the optimisation engine.</summary>
public sealed record CuttingSheetSubmittedEvent(
    Guid TenantId,
    Guid SheetId,
    Guid SourceOrderId,
    int LineCount,
    DateTime OccurredAt);
