using System.Net;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class InternalEndpointsTests : IDisposable
{
    private static readonly Guid AllowedTenant = new("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly HttpClient _client;
    private readonly Mock<ICuttingRepository> _repoMock = new();

    public InternalEndpointsTests()
    {
        Environment.SetEnvironmentVariable(
            "TEST_TENANT_ALLOWLIST", AllowedTenant.ToString());

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(_repoMock.Object);
        builder.Services.AddSingleton(new Mock<IMediator>().Object);
        builder.Services.AddDbContext<CuttingDbContext>(opts =>
            opts.UseInMemoryDatabase("test-cutting-internal"));
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthorization();
        app.MapInternalEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        _client = testServer!.CreateClient();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("TEST_TENANT_ALLOWLIST", null);
        _client.Dispose();
    }

    // ── Header check ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_MissingInternalHeader_Returns403()
    {
        var response = await _client.DeleteAsync(
            $"/internal/cutting-sheets/by-tenant/{AllowedTenant}?confirm=true");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WrongHeaderValue_Returns403()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/cutting-sheets/by-tenant/{AllowedTenant}?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "false");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Confirm param check ───────────────────────────────────────────────────

    [Fact]
    public async Task Delete_MissingConfirm_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/cutting-sheets/by-tenant/{AllowedTenant}");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GUID format check ─────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_InvalidGuid_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "/internal/cutting-sheets/by-tenant/not-a-guid?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Allowlist check ───────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_TenantNotInAllowlist_Returns403()
    {
        var unknownTenant = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/cutting-sheets/by-tenant/{unknownTenant}?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── IngestOrder endpoint ────────────────────────────────────────────────

    [Fact]
    public async Task IngestOrder_MissingInternalHeader_Returns403()
    {
        var body = new StringContent(
            """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","items":[]}""",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/internal/ingest-order", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Success ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ValidRequest_Returns200WithCounts()
    {
        _repoMock
            .Setup(r => r.DeleteByTenantAsync(AllowedTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 1));

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/internal/cutting-sheets/by-tenant/{AllowedTenant}?confirm=true");
        request.Headers.Add("X-SpaceOS-Internal", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("cuttingSheets");
        body.Should().Contain("dailyCuttingPlans");
    }
}
