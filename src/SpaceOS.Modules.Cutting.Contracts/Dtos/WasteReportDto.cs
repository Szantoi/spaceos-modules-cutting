namespace SpaceOS.Modules.Cutting.Contracts.Dtos;

/// <summary>Aggregated waste statistics over a date range, broken down by material type.</summary>
public sealed record WasteReportDto(
    decimal TotalAreaCut,
    decimal TotalWasteArea,
    decimal WastePercentage,
    IReadOnlyList<WasteLineDto> Lines);

/// <summary>Waste figures for a single material type within a report period.</summary>
public sealed record WasteLineDto(string MaterialType, decimal WasteArea, decimal WastePercentage);
