using System.Net.Http.Json;
using SpaceOS.Modules.Cutting.Application.Events;

namespace SpaceOS.Modules.Cutting.Infrastructure.Events;

/// <summary>
/// Fires cross-service integration events from CUTTING (:5005) → INVENTORY (:5004)
/// via HTTP POST to the Inventory integration endpoint.
/// </summary>
public sealed class CuttingEventPublisher : ICuttingEventPublisher
{
    private readonly HttpClient _httpClient;

    public CuttingEventPublisher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task PublishJobCompletedAsync(
        Guid jobId,
        Guid tenantId,
        Guid cuttingSheetId,
        DateTime completedAt,
        decimal yieldPct,
        decimal wasteM2,
        CancellationToken ct = default)
    {
        var payload = new
        {
            jobId,
            tenantId,
            cuttingSheetId,
            completedAt,
            yieldPct,
            wasteM2
        };

        using var requestMsg = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/inventory/integration/cutting-job-completed");
        requestMsg.Content = JsonContent.Create(payload);
        requestMsg.Headers.Add("X-Internal-Service", "cutting");

        var response = await _httpClient
            .SendAsync(requestMsg, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
