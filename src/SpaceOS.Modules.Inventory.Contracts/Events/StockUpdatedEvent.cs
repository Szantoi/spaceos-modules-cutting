namespace SpaceOS.Modules.Inventory.Contracts.Events;

/// <summary>Raised after any stock movement (consumption or inbound delivery) is recorded.</summary>
public sealed record StockUpdatedEvent(
    Guid TenantId,
    string MaterialType,
    decimal Thickness,
    int NewPanelCount,
    string MovementReason,
    DateTime OccurredAt);
