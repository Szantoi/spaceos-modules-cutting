using System.Net;
using System.Net.Http.Json;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Application.Adapters.Commands;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;
using SpaceOS.Modules.Cutting.Application.Adapters.Queries;
using SpaceOS.Modules.Cutting.Tests.Api;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Api;

public class AdapterAdminEndpointsTests
{
    private HttpClient CreateAuthClient(Mock<IMediator> mediatorMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAdapterAdminEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    private HttpClient CreateNoAuthClient(Mock<IMediator> mediatorMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddAuthentication("NoAuth")
            .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("NoAuth", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAdapterAdminEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        return testServer!.CreateClient();
    }

    [Fact]
    public async Task PostConfig_ValidRequest_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<ConfigureAdapterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = CreateAuthClient(mediatorMock);
        var payload = new { AdapterName = "builtin", TransportName = "none", ConfigJson = "{}", ConfigSchemaVersion = 1, ExpectedVersion = 0 };

        var response = await client.PostAsJsonAsync("/api/cutting/adapters/config", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostConfig_ValidationError_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<ConfigureAdapterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Invalid(new ValidationError("AdapterName is invalid.")));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { AdapterName = "bad-adapter", TransportName = "none", ConfigJson = "{}", ConfigSchemaVersion = 1, ExpectedVersion = 0 };

        var response = await client.PostAsJsonAsync("/api/cutting/adapters/config", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostConfig_VersionConflict_Returns409()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<ConfigureAdapterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Conflict("Version mismatch."));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { AdapterName = "builtin", TransportName = "none", ConfigJson = "{}", ConfigSchemaVersion = 1, ExpectedVersion = 5 };

        var response = await client.PostAsJsonAsync("/api/cutting/adapters/config", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetConfig_ConfigExists_Returns200WithDto()
    {
        var tenantId = Guid.NewGuid();
        var dto = new AdapterConfigDto(tenantId, "builtin", "none", true, 1, DateTimeOffset.UtcNow);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetAdapterConfigQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterConfigDto>.Success(dto));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync("/api/cutting/adapters/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConfig_NoConfig_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetAdapterConfigQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterConfigDto>.NotFound());

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync("/api/cutting/adapters/config");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostConfigTest_HealthyAdapter_Returns200()
    {
        var dto = new AdapterTestResultDto(true, "OK");
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<TestAdapterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterTestResultDto>.Success(dto));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.PostAsJsonAsync("/api/cutting/adapters/config/test", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_HealthRecordExists_Returns200()
    {
        var dto = new AdapterHealthDto(Guid.NewGuid(), "builtin", true, DateTimeOffset.UtcNow, null, 0);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetAdapterHealthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterHealthDto>.Success(dto));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync("/api/cutting/adapters/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostConfig_WithoutAuth_Returns401()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateNoAuthClient(mediatorMock);

        var response = await client.PostAsJsonAsync("/api/cutting/adapters/config", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfig_WithoutAuth_Returns401()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateNoAuthClient(mediatorMock);

        var response = await client.GetAsync("/api/cutting/adapters/config");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHealth_WithoutAuth_Returns401()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateNoAuthClient(mediatorMock);

        var response = await client.GetAsync("/api/cutting/adapters/health");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
