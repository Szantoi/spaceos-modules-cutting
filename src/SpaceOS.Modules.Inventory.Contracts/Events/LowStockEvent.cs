namespace SpaceOS.Modules.Inventory.Contracts.Events;

/// <summary>Raised when stock falls at or below the configured threshold for a material type.</summary>
public sealed record LowStockEvent(
    Guid TenantId,
    string MaterialType,
    int CurrentPanelCount,
    int ThresholdPanelCount,
    DateTime OccurredAt);
