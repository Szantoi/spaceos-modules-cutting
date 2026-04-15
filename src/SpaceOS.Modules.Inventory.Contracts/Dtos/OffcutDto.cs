namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Represents a leftover panel offcut available for reuse in cutting optimisation.</summary>
public sealed record OffcutDto(
    Guid Id,
    decimal Width,
    decimal Height,
    decimal Thickness,
    string MaterialType,
    Guid OriginSheetId);
