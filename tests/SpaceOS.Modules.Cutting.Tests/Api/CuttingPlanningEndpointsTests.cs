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
using SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Commands.UpdateCuttingPlanStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class CuttingPlanningEndpointsTests
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
        app.MapCuttingPlanningEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    [Fact]
    public async Task CreateCuttingPlan_WithValidPayload_Returns201()
    {
        var mediatorMock = new Mock<IMediator>();
        var planId = Guid.NewGuid();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCuttingPlanResponse>.Success(
                new CreateCuttingPlanResponse(planId, new List<DailyPlanResponse>(), new List<CuttingJobResponse>(), 91m)));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = DateTime.UtcNow.ToString("yyyy-MM-dd"), planDays = 14, strategyId = "maxcut-v1" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCuttingPlan_WithInvalidDate_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = "not-a-date", planDays = 14, strategyId = "maxcut-v1" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCuttingPlan_WhenHandlerReturnsInvalid_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCuttingPlanResponse>.Invalid(new ValidationError("PlanDays must be 7-90.")));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = DateTime.UtcNow.ToString("yyyy-MM-dd"), planDays = 14, strategyId = "maxcut-v1" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCuttingPlan_Existing_Returns200()
    {
        var planId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCuttingPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CuttingPlanResponse>.Success(
                new CuttingPlanResponse(planId, DateTime.UtcNow.ToString("yyyy-MM-dd"), 14, "Draft", "maxcut-v1", new List<DailyPlanResponse>())));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/planning/{planId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCuttingPlan_NotFound_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCuttingPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CuttingPlanResponse>.NotFound("Not found"));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/planning/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCuttingPlans_Returns200()
    {
        var mediatorMock = new Mock<IMediator>();
        var repoMock = new Mock<ICuttingRepository>();
        repoMock
            .Setup(r => r.GetAllCuttingPlansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpaceOS.Modules.Cutting.Domain.Aggregates.CuttingPlan>());

        var client = CreateAuthClient(mediatorMock, repoMock);
        var response = await client.GetAsync("/api/cutting/planning/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCuttingPlan_WithValidStatus_Returns200()
    {
        var planId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateCuttingPlanStatusCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediatR.Unit>.Success(MediatR.Unit.Value));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { status = "Approved" };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/{planId}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCuttingPlan_NotFound_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateCuttingPlanStatusCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediatR.Unit>.NotFound("Plan not found."));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { status = "Approved" };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCuttingPlan_WithoutStatus_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateAuthClient(mediatorMock);
        var payload = new { status = (string?)null };
        var response = await client.PutAsJsonAsync($"/api/cutting/planning/{Guid.NewGuid()}", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDailyPlan_WithInvalidDate_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/planning/{Guid.NewGuid()}/daily/not-a-date");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDailyPlan_PlanNotFound_Returns404()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCuttingPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CuttingPlanResponse>.NotFound("Not found"));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/planning/{Guid.NewGuid()}/daily/{DateTime.UtcNow:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCuttingPlan_WithoutAuth_Returns401()
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
        app.MapCuttingPlanningEndpoints();
        await app.StartAsync();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = testServer!.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cutting/planning/", new { planDate = "2026-05-01", planDays = 14, strategyId = "fifo" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
