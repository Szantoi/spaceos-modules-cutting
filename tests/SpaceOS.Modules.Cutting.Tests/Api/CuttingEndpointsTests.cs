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
using SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;
using SpaceOS.Modules.Cutting.Application.Commands.CreateDailyCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;
using SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;
using SpaceOS.Modules.Cutting.Application.Queries.GetDailyCuttingPlan;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class CuttingEndpointsTests
{
    private HttpClient CreateAuthClient(Mock<IMediator> mediatorMock, Mock<ICuttingRepository>? repoMock = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddSingleton((repoMock ?? new Mock<ICuttingRepository>()).Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCuttingEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    [Fact]
    public async Task SubmitCuttingSheet_WithAuth_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<SubmitCuttingSheetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var client = CreateAuthClient(mediatorMock);
        var payload = new
        {
            OrderReference = "ORD-001",
            Lines = new[]
            {
                new { PartName = "P1", MaterialType = "MDF 18mm", WidthMm = 600m, HeightMm = 400m, ThicknessMm = 18m, Quantity = 2, Notes = (string?)null }
            }
        };
        var response = await client.PostAsJsonAsync("/api/cutting/sheets", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitCuttingSheet_WithoutAuth_Returns401()
    {
        var mediatorMock = new Mock<IMediator>();
        var repoMock = new Mock<ICuttingRepository>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddSingleton(repoMock.Object);
        builder.Services.AddAuthentication("NoAuth")
            .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("NoAuth", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));
        builder.Services.AddRouting();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCuttingEndpoints();
        await app.StartAsync();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cutting/sheets", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNestingResult_ExistingSheet_Returns200()
    {
        var sheetId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetNestingResultQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NestingResultResponse>.Success(
                new NestingResultResponse(sheetId, "ORD-001", new List<NestingGroupResponse>(), 0)));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/sheets/{sheetId}/nesting");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNestingResult_NotFound_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetNestingResultQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NestingResultResponse>.NotFound("Not found"));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/sheets/{Guid.NewGuid()}/nesting");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetExecutionStatus_Returns200()
    {
        var sheetId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetExecutionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExecutionStatusResponse>.Success(
                new ExecutionStatusResponse(sheetId, "Completed", null, null, 0m)));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/sheets/{sheetId}/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWasteReport_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetWasteReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WasteReportResponse>.Success(new WasteReportResponse(0m, 0m, 0)));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync("/api/cutting/waste");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateDailyCuttingPlan_WithAuth_Returns201()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<CreateDailyCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var client = CreateAuthClient(mediatorMock);
        var payload = new
        {
            Name = "Teszt vágóterv 2026-04-18",
            Date = "2026-04-18"
        };
        var response = await client.PostAsJsonAsync("/api/cutting/plans", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetDailyCuttingPlan_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetDailyCuttingPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DailyCuttingPlanResponse>.Success(
                new DailyCuttingPlanResponse(Guid.NewGuid(), "Test Plan", DateTime.UtcNow.Date, "Draft", new List<CuttingBatchResponse>())));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/plans/{DateTime.UtcNow:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDailyCuttingPlan_InvalidDate_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync("/api/cutting/plans/not-a-date");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetExecutionStatus_NotFound_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetExecutionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExecutionStatusResponse>.NotFound("Not found"));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/sheets/{Guid.NewGuid()}/status");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
