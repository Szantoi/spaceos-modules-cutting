namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Aggregated daily consumption trend for a given material type over a date range.</summary>
public sealed record ConsumptionTrendDto(
    string MaterialType,
    IReadOnlyList<DailyConsumptionDto> DailyData,
    decimal AverageDailyConsumption);

/// <summary>Area consumed for a single calendar day.</summary>
public sealed record DailyConsumptionDto(DateTime Date, decimal Area);
