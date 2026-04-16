using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure;

public class InventoryProviderHttpAdapterTests
{
    private static InventoryProviderHttpAdapter CreateAdapter(HttpStatusCode status, object? content = null)
    {
        var handler = new MockHttpMessageHandler(status, content);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5004") };
        return new InventoryProviderHttpAdapter(httpClient,
            NullLogger<InventoryProviderHttpAdapter>.Instance);
    }

    private static InventoryProviderHttpAdapter CreateThrowingAdapter()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler()) { BaseAddress = new Uri("http://localhost:5004") };
        return new InventoryProviderHttpAdapter(httpClient,
            NullLogger<InventoryProviderHttpAdapter>.Instance);
    }

    // ── GetStockAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockAsync_WhenInventoryReturnsData_ReturnsPanelStockDto()
    {
        var json = new { materialType = "MDF 18mm", fullPanelCount = 5, widthMm = 2800, heightMm = 2070, offcutCount = 3 };
        var adapter = CreateAdapter(HttpStatusCode.OK, json);

        var result = await adapter.GetStockAsync("MDF 18mm");

        result.MaterialType.Should().Be("MDF 18mm");
        result.FullPanelCount.Should().Be(5);
        result.WidthMm.Should().Be(2800);
        result.HeightMm.Should().Be(2070);
        result.Offcuts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStockAsync_WhenInventoryReturns503_ReturnsEmptyDefault()
    {
        var adapter = CreateAdapter(HttpStatusCode.ServiceUnavailable);

        var result = await adapter.GetStockAsync("MDF 18mm");

        result.MaterialType.Should().Be("MDF 18mm");
        result.FullPanelCount.Should().Be(0);
        result.Offcuts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStockAsync_WhenNetworkThrows_ReturnsEmptyDefault()
    {
        var adapter = CreateThrowingAdapter();

        var result = await adapter.GetStockAsync("MDF 18mm");

        result.FullPanelCount.Should().Be(0);
    }

    // ── GetOffcutsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOffcutsAsync_WhenInventoryReturnsData_ReturnsOffcutList()
    {
        var offcutId = Guid.NewGuid();
        var originId = Guid.NewGuid();
        var json = new[]
        {
            new { id = offcutId, widthMm = 500.0m, heightMm = 300.0m, materialCatalogId = Guid.NewGuid(), originCuttingSheetId = (Guid?)originId }
        };
        var adapter = CreateAdapter(HttpStatusCode.OK, json);

        var result = await adapter.GetOffcutsAsync("MDF 18mm");

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(offcutId);
        result[0].MaterialType.Should().Be("MDF 18mm");
        result[0].WidthMm.Should().Be(500);
        result[0].HeightMm.Should().Be(300);
        result[0].OriginCuttingSheetId.Should().Be(originId);
    }

    [Fact]
    public async Task GetOffcutsAsync_WhenInventoryUnavailable_ReturnsEmpty()
    {
        var adapter = CreateAdapter(HttpStatusCode.InternalServerError);

        var result = await adapter.GetOffcutsAsync("MDF 18mm");

        result.Should().BeEmpty();
    }

    // ── RecordConsumptionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RecordConsumptionAsync_WhenInventoryUnavailable_DoesNotThrow()
    {
        var adapter = CreateAdapter(HttpStatusCode.ServiceUnavailable);
        var items = new List<StockMovementDto>
        {
            new("MDF 18mm", 18m, 5.5m, 1, "Gyártás", DateTime.UtcNow)
        };

        var act = () => adapter.RecordConsumptionAsync(items);

        await act.Should().NotThrowAsync();
    }

    // ── GetConsumptionTrendAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetConsumptionTrendAsync_WhenInventoryUnavailable_ReturnsEmptyDefault()
    {
        var adapter = CreateAdapter(HttpStatusCode.ServiceUnavailable);
        var range = new DateRange(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await adapter.GetConsumptionTrendAsync(range);

        result.AverageDailyConsumption.Should().Be(0m);
        result.DailyData.Should().BeEmpty();
    }
}

// ── test doubles ─────────────────────────────────────────────────────────────

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly object? _content;

    public MockHttpMessageHandler(HttpStatusCode status, object? content = null)
    {
        _status = status;
        _content = content;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var msg = new HttpResponseMessage(_status);
        if (_content is not null)
            msg.Content = JsonContent.Create(_content);
        return await Task.FromResult(msg).ConfigureAwait(false);
    }
}

internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => throw new HttpRequestException("Network error");
}
