namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Catalog entry describing the standard dimensions of a material type.</summary>
public sealed record MaterialCatalogDto(
    string MaterialType,
    string Description,
    decimal StandardThickness,
    decimal PanelWidth,
    decimal PanelHeight);
