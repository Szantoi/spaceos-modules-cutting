namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Current stock level for a given material type, including available offcuts.</summary>
public sealed record PanelStockDto(
    string MaterialType,
    decimal Thickness,
    int FullPanelCount,
    IReadOnlyList<OffcutDto> Offcuts);
