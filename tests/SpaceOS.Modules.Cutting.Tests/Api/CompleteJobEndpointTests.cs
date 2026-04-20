using System.Net;
using System.Net.Http.Json;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Application.Commands.CompleteJob;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class CompleteJobEndpointTests
{
    private static readonly Guid TestJobId = Guid.NewGuid();

    private HttpClient CreateAuthClient(Mock<IMediator> mediatorMock)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddSingleton(new Mock<ICuttingRepository>().Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCuttingPlanningEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    // ── 200 OK ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_CompleteJob_WithValidJob_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CompleteJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Unit>.Success(Unit.Value));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { cuttingSheetId = Guid.NewGuid(), yieldPct = 91m, wasteM2 = 0.5m };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/jobs/{TestJobId}/complete", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_CompleteJob_PassesCorrectCommandToMediator()
    {
        CompleteJobCommand? captured = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CompleteJobCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<Unit>>, CancellationToken>((cmd, _) => captured = (CompleteJobCommand)cmd)
            .ReturnsAsync(Result<Unit>.Success(Unit.Value));

        var client = CreateAuthClient(mediatorMock);
        var sheetId = Guid.NewGuid();
        var payload = new { cuttingSheetId = sheetId, yieldPct = 88m, wasteM2 = 1.2m };
        await client.PutAsJsonAsync($"/api/cutting/planning/jobs/{TestJobId}/complete", payload);

        captured.Should().NotBeNull();
        captured!.JobId.Should().Be(TestJobId);
        captured.YieldPct.Should().Be(88m);
        captured.WasteM2.Should().Be(1.2m);
    }

    // ── 404 Not Found ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_CompleteJob_WhenJobNotFound_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CompleteJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Unit>.NotFound("Job not found."));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { yieldPct = 91m, wasteM2 = 0.5m };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/jobs/{TestJobId}/complete", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── 400 Bad Request (invalid state) ───────────────────────────────────────

    [Fact]
    public async Task Put_CompleteJob_WhenAlreadyCut_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CompleteJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Unit>.Invalid(new ValidationError("Job is already 'Cut'.")));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { yieldPct = 91m, wasteM2 = 0.5m };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/jobs/{TestJobId}/complete", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 401 Unauthorized ─────────────────────────────────────────────────────

    [Fact]
    public async Task Put_CompleteJob_WithoutAuth_Returns401()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(new Mock<IMediator>().Object);
        builder.Services.AddSingleton(new Mock<ICuttingRepository>().Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCuttingPlanningEndpoints();
        await app.StartAsync();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();

        var payload = new { yieldPct = 91m, wasteM2 = 0.5m };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/jobs/{TestJobId}/complete", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
