using System.Net.Http.Json;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// HTTP adapter for registering offcuts in the Inventory service.
/// Retries up to 3 times with exponential backoff (1s, 2s, 4s) on transient failures.
/// CorrelationId = PlanId for idempotency.
/// </summary>
internal sealed class InventoryCuttingHttpAdapter : IInventoryCuttingAdapter
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    private readonly HttpClient _http;
    private readonly ILogger<InventoryCuttingHttpAdapter> _logger;

    public InventoryCuttingHttpAdapter(HttpClient http, ILogger<InventoryCuttingHttpAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<Result> RegisterOffcutsAsync(
        Guid planId,
        Guid tenantId,
        IReadOnlyList<OffcutRegistrationItem> items,
        CancellationToken ct)
    {
        var body = new
        {
            CorrelationId = planId,
            Items = items.Select(i => new
            {
                i.MaterialCode,
                i.WidthMm,
                i.HeightMm,
                i.X,
                i.Y
            }).ToList()
        };

        Exception? lastEx = null;

        for (int attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            try
            {
                using var response = await _http.PostAsJsonAsync("/api/inventory/offcuts/batch", body, ct)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    return Result.Success();

                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "InventoryCuttingHttpAdapter: attempt {Attempt} failed with status {Status}: {Error}",
                    attempt + 1, response.StatusCode, error);

                // Do not retry on client errors (4xx)
                if ((int)response.StatusCode < 500)
                    return Result.Error($"Offcut batch registration failed: {response.StatusCode}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastEx = ex;
                _logger.LogWarning(ex,
                    "InventoryCuttingHttpAdapter: attempt {Attempt} threw transient exception.", attempt + 1);
            }

            if (attempt < RetryDelays.Length)
                await Task.Delay(RetryDelays[attempt], ct).ConfigureAwait(false);
        }

        return Result.Error($"Offcut batch registration failed after {RetryDelays.Length + 1} attempts: {lastEx?.Message}");
    }
}
