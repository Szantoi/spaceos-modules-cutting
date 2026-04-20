using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
using SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class CuttingPlanningStrategyEndpointsTests
{
    private static readonly Guid TestPlanId = Guid.NewGuid();
    private static readonly string TodayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

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

    private static CreateCuttingPlanResponse MakeResponse(decimal yield = 91m)
        => new(TestPlanId, new List<DailyPlanResponse>(), new List<CuttingJobResponse>(), yield);

    [Fact]
    public async Task Post_WithMaxcutV1Strategy_Returns201()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCuttingPlanResponse>.Success(MakeResponse()));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = TodayStr, planDays = 14, strategyId = "maxcut-v1" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_ResponseBodyIncludesYieldField()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCuttingPlanResponse>.Success(MakeResponse(91.5m)));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = TodayStr, planDays = 14, strategyId = "maxcut-v1" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalYieldPercent").GetDecimal().Should().Be(91.5m);
    }

    [Theory]
    [InlineData("fifo")]
    [InlineData("priority")]
    [InlineData("custom")]
    public async Task Post_WithValidStrategy_Returns201(string strategyId)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCuttingPlanResponse>.Success(MakeResponse(75m)));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = TodayStr, planDays = 14, strategyId };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_WhenHandlerReturnsInvalidForStrategy_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCuttingPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCuttingPlanResponse>.Invalid(
                new ValidationError("Unknown planning strategy: 'bad-strategy'.")));

        var client = CreateAuthClient(mediatorMock);
        var payload = new { planDate = TodayStr, planDays = 14, strategyId = "bad-strategy" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ExistingPlan_ReturnsPlanWithFields()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCuttingPlanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CuttingPlanResponse>.Success(
                new CuttingPlanResponse(TestPlanId, TodayStr, 14, "Draft", "maxcut-v1",
                    new List<DailyPlanResponse>())));

        var client = CreateAuthClient(mediatorMock);
        var response = await client.GetAsync($"/api/cutting/planning/{TestPlanId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("strategyId").GetString().Should().Be("maxcut-v1");
    }

    [Fact]
    public async Task Post_WithEmptyRequestBody_Returns400()
    {
        var mediatorMock = new Mock<IMediator>();
        var client = CreateAuthClient(mediatorMock);

        // Send only required field missing (planDate) as null
        var payload = new { planDate = (string?)null, planDays = 14, strategyId = "maxcut-v1" };
        var response = await client.PostAsJsonAsync("/api/cutting/planning/", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
