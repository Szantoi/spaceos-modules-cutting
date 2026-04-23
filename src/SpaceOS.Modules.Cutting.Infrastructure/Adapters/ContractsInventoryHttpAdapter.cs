using System.Net.Http.Json;
using System.Text.Json;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Contracts.Inventory;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Contracts.Inventory.Requests;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// HTTP adapter for <see cref="IInventoryProvider"/> (SpaceOS.Modules.Contracts v1.3.0).
/// Cutting module uses only <see cref="ReserveAsync"/> and <see cref="ReleaseReservationAsync"/>.
/// Other methods are declared but not needed by this consumer.
/// </summary>
internal sealed class ContractsInventoryHttpAdapter : IInventoryProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<ContractsInventoryHttpAdapter> _logger;

    public string ProviderName => "Cutting.InventoryReservation.Http";

    public ProviderCapability Capabilities => ProviderCapability.InventoryReservation;

    public ContractsInventoryHttpAdapter(HttpClient http, ILogger<ContractsInventoryHttpAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ReservationDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct)
    {
        try
        {
            var body = new
            {
                request.CorrelationId,
                request.ConsumerModule,
                ConsumerContextJson = (string?)null,
                CreatedByUserId = (Guid?)null,
                Items = request.Items.Select(i => new
                {
                    i.StockItemId,
                    i.MaterialCode,
                    Quantity = i.QuantityReserved
                }).ToList(),
                Ttl = request.Ttl.ToString()
            };

            using var response = await _http.PostAsJsonAsync("/api/inventory/reservations", body, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Inventory reserve failed {Status}: {Error}", response.StatusCode, error);
                return Result<ReservationDto>.Error($"Inventory reservation failed with status {response.StatusCode}.");
            }

            var dto = await response.Content.ReadFromJsonAsync<ReservationDto>(
                cancellationToken: ct).ConfigureAwait(false);

            if (dto is null)
                return Result<ReservationDto>.Error("Inventory returned null reservation.");

            return Result<ReservationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReserveAsync HTTP call failed for correlation {CorrelationId}", request.CorrelationId);
            return Result<ReservationDto>.Error($"Inventory reservation call failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ReleaseReservationAsync(Guid correlationId, string? reason, CancellationToken ct)
    {
        try
        {
            var url = $"/api/inventory/reservations/{correlationId}";
            if (!string.IsNullOrWhiteSpace(reason))
                url += $"?reason={Uri.EscapeDataString(reason)}";

            using var response = await _http.DeleteAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Inventory release failed {Status} for {CorrelationId}", response.StatusCode, correlationId);
                return Result.Error($"Release failed with status {response.StatusCode}.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReleaseReservationAsync failed for {CorrelationId}", correlationId);
            return Result.Error($"Release call failed: {ex.Message}");
        }
    }

    // ── Methods not used by Cutting module ───────────────────────────────────

    /// <inheritdoc />
    public Task<bool> HealthCheckAsync(CancellationToken ct)
        => Task.FromResult(true);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StockItemDto>>> GetStockAsync(string materialCode, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(GetStockAsync)} is not used by the Cutting module. Use the dedicated InventoryProviderHttpAdapter.");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StockItemDto>>> GetUsableOffcutsAsync(
        string materialCode, decimal minWidth, decimal minHeight, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(GetUsableOffcutsAsync)} is not used by the Cutting module.");

    /// <inheritdoc />
    public Task<Result> RecordConsumptionAsync(
        IReadOnlyList<SpaceOS.Modules.Contracts.Inventory.DTOs.StockMovementDto> movements, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(RecordConsumptionAsync)} is not used by the Cutting module.");

    /// <inheritdoc />
    public Task<Result<Guid>> RecordOffcutAsync(
        SpaceOS.Modules.Contracts.Cutting.DTOs.CuttingOffcutResultDto offcut, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(RecordOffcutAsync)} is not used by the Cutting module.");

    /// <inheritdoc />
    public Task<Result> RecordInboundAsync(
        IReadOnlyList<SpaceOS.Modules.Contracts.Inventory.Requests.InboundReceiptDto> items, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(RecordInboundAsync)} is not used by the Cutting module.");

    /// <inheritdoc />
    public Task<Result<SpaceOS.Modules.Contracts.Inventory.DTOs.ConsumptionTrendDto>> GetConsumptionTrendAsync(
        string materialCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(GetConsumptionTrendAsync)} is not used by the Cutting module.");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ReservationDto>>> GetReservationsAsync(
        ReservationFilter filter, CancellationToken ct)
        => throw new NotSupportedException($"{nameof(GetReservationsAsync)} is not used by the Cutting module.");
}
