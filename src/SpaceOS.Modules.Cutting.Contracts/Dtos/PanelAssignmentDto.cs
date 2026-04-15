namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>Nesting result for a submitted cutting sheet, including panel count and per-part placements.</summary>
public sealed record PanelAssignmentDto(
    Guid SheetId,
    IReadOnlyList<PanelPlacementDto> Placements,
    decimal WastePercentage,
    int PanelsRequired);

/// <summary>Position and orientation of a single part on a panel.</summary>
public sealed record PanelPlacementDto(
    string PartName,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    bool IsRotated);
