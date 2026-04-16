namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

public sealed record PlacedPart(
    string PartName,
    int X,
    int Y,
    int WidthMm,
    int HeightMm,
    bool IsRotated);
