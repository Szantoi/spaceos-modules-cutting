namespace SpaceOS.Modules.Cutting.Domain.Services;

public sealed record AvailablePanel(
    Guid PanelStockId,
    string MaterialType,
    decimal WidthMm,
    decimal HeightMm,
    bool IsOffcut);
