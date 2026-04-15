namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Represents an inclusive date range used for trend and report queries.</summary>
public sealed record DateRange(DateTime From, DateTime To);
