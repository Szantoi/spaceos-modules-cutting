using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Inventory.Contracts.Providers;

/// <summary>
/// Contract for querying and mutating panel stock, offcuts, and consumption trends.
/// Implementations live in the Inventory module — callers depend only on this interface.
/// </summary>
public interface IInventoryProvider
{
    /// <summary>Returns the current stock level for the given material type.</summary>
    Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default);

    /// <summary>Returns all offcuts available for reuse for the given material type.</summary>
    Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default);

    /// <summary>Records one or more consumption movements against stock.</summary>
    Task RecordConsumptionAsync(IReadOnlyList<StockMovementDto> items, CancellationToken ct = default);

    /// <summary>Records a single inbound delivery to stock.</summary>
    Task RecordInboundAsync(StockMovementDto delivery, CancellationToken ct = default);

    /// <summary>Records a new offcut produced by a cutting operation.</summary>
    Task RecordOffcutAsync(OffcutDto offcut, CancellationToken ct = default);

    /// <summary>Returns aggregated daily consumption data for the specified date range.</summary>
    Task<ConsumptionTrendDto> GetConsumptionTrendAsync(DateRange range, CancellationToken ct = default);
}
