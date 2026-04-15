namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Describes a single stock movement — either consumption or inbound delivery.</summary>
public sealed record StockMovementDto(
    string MaterialType,
    decimal Thickness,
    decimal Area,
    int PanelCount,
    string Reason,
    DateTime OccurredAt);
