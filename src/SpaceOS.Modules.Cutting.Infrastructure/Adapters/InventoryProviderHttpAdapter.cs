using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using SpaceOS.Modules.Inventory.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// HTTP adapter that calls the Inventory service over loopback.
/// Every method degrades gracefully — callers (e.g. nesting handler) handle empty data.
/// </summary>
internal sealed class InventoryProviderHttpAdapter : IInventoryProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<InventoryProviderHttpAdapter> _logger;

    public InventoryProviderHttpAdapter(HttpClient http, ILogger<InventoryProviderHttpAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<StockApiResponse>(
                $"/api/inventory/stock?materialType={Uri.EscapeDataString(materialType)}", ct)
                .ConfigureAwait(false);
            return response is null
                ? new PanelStockDto(materialType, 0, 0, 0, Array.Empty<OffcutDto>())
                : new PanelStockDto(materialType, response.FullPanelCount, response.WidthMm, response.HeightMm, Array.Empty<OffcutDto>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory service unavailable for stock {MaterialType}", materialType);
            return new PanelStockDto(materialType, 0, 0, 0, Array.Empty<OffcutDto>());
        }
    }

    public async Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<OffcutApiResponse>>(
                $"/api/inventory/offcuts?materialType={Uri.EscapeDataString(materialType)}", ct)
                .ConfigureAwait(false);
            if (response is null) return Array.Empty<OffcutDto>();
            return response.Select(o => new OffcutDto(
                o.Id,
                materialType,
                (int)o.WidthMm,
                (int)o.HeightMm,
                o.OriginCuttingSheetId ?? Guid.Empty)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory service unavailable for offcuts {MaterialType}", materialType);
            return Array.Empty<OffcutDto>();
        }
    }

    public async Task RecordConsumptionAsync(IReadOnlyList<StockMovementDto> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            try
            {
                var body = new
                {
                    item.MaterialType,
                    item.Thickness,
                    item.Area,
                    item.PanelCount,
                    item.Reason,
                    item.OccurredAt
                };
                using var result = await _http.PostAsJsonAsync("/api/inventory/movements/consumption", body, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record consumption for {MaterialType}", item.MaterialType);
            }
        }
    }

    public async Task RecordInboundAsync(StockMovementDto delivery, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                delivery.MaterialType,
                delivery.Thickness,
                delivery.Area,
                delivery.PanelCount,
                Reference = delivery.Reason,
                delivery.OccurredAt
            };
            using var result = await _http.PostAsJsonAsync("/api/inventory/movements/inbound", body, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record inbound for {MaterialType}", delivery.MaterialType);
        }
    }

    public async Task RecordOffcutAsync(OffcutDto offcut, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                offcut.MaterialType,
                offcut.WidthMm,
                offcut.HeightMm,
                OriginCuttingSheetId = offcut.OriginCuttingSheetId == Guid.Empty ? (Guid?)null : offcut.OriginCuttingSheetId
            };
            using var result = await _http.PostAsJsonAsync("/api/inventory/movements/offcut", body, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record offcut for {MaterialType}", offcut.MaterialType);
        }
    }

    public async Task<ConsumptionTrendDto> GetConsumptionTrendAsync(DateRange range, CancellationToken ct = default)
    {
        try
        {
            var from = range.From.ToString("O");
            var to = range.To.ToString("O");
            var response = await _http.GetFromJsonAsync<TrendApiResponse>(
                $"/api/inventory/trend?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}", ct)
                .ConfigureAwait(false);
            if (response is null) return new ConsumptionTrendDto(string.Empty, Array.Empty<DailyConsumptionDto>(), 0m);
            var daily = response.DailyData.Select(d => new DailyConsumptionDto(d.Date, d.Area)).ToList();
            return new ConsumptionTrendDto(response.MaterialType, daily, response.AverageDailyConsumption);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory service unavailable for trend {From}-{To}", range.From, range.To);
            return new ConsumptionTrendDto(string.Empty, Array.Empty<DailyConsumptionDto>(), 0m);
        }
    }

    // ── private API response shapes ──────────────────────────────────────────

    private sealed record StockApiResponse(string MaterialType, int FullPanelCount, int WidthMm, int HeightMm, int OffcutCount);

    private sealed record OffcutApiResponse(Guid Id, decimal WidthMm, decimal HeightMm, Guid MaterialCatalogId, Guid? OriginCuttingSheetId);

    private sealed record TrendApiResponse(string MaterialType, IReadOnlyList<TrendEntry> DailyData, decimal AverageDailyConsumption);

    private sealed record TrendEntry(DateTime Date, decimal Area);
}
