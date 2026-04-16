using SpaceOS.Modules.Inventory.Contracts.Dtos;
using SpaceOS.Modules.Inventory.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// Stub implementation of IInventoryProvider for the cutting service.
/// Returns empty stock — the nesting handler degrades gracefully (grouping-only response).
/// Replace with HTTP adapter when cross-service inventory calls are implemented (Q3).
/// </summary>
internal sealed class InventoryProviderStub : IInventoryProvider
{
    public Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default)
        => Task.FromResult(new PanelStockDto(materialType, FullPanelCount: 0, WidthMm: 0, HeightMm: 0, Offcuts: Array.Empty<OffcutDto>()));

    public Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OffcutDto>>(Array.Empty<OffcutDto>());

    public Task RecordConsumptionAsync(IReadOnlyList<StockMovementDto> items, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RecordInboundAsync(StockMovementDto delivery, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RecordOffcutAsync(OffcutDto offcut, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<ConsumptionTrendDto> GetConsumptionTrendAsync(DateRange range, CancellationToken ct = default)
        => Task.FromResult(new ConsumptionTrendDto(string.Empty, Array.Empty<DailyConsumptionDto>(), 0m));
}
