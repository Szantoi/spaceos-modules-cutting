namespace SpaceOS.Modules.Cutting.Domain.Services;

public sealed record CuttingLineRequest(
    string PartName,
    decimal WidthMm,
    decimal HeightMm,
    bool CanRotate = true);
