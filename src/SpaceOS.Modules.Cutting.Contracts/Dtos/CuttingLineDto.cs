namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>A single part line on a cutting sheet, describing raw dimensions and edge banding requirements.</summary>
public sealed record CuttingLineDto(
    string Name,
    string PartType,
    decimal RawWidth,
    decimal RawHeight,
    decimal Thickness,
    int Quantity,
    bool CanRotate,
    string? EdgeBanding);
