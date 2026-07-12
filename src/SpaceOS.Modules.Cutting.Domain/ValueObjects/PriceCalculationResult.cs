namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>
/// Value object representing the result of a price calculation with breakdown.
/// </summary>
public record PriceCalculationResult(decimal Price, string Breakdown);
