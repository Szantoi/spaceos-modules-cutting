using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Api;

public class ScheduleExecutionEndpointTests : IDisposable
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly HttpClient _client;
    private readonly Mock<IMediator> _mediatorMock = new();

    public ScheduleExecutionEndpointTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(_mediatorMock.Object);
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("ManufacturerOnly", p => p.RequireAuthenticatedUser()));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCuttingExecutionEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer;
        _client = testServer!.CreateClient();
    }

    [Fact]
    public async Task Post_Executions_WithValidRequest_Returns201()
    {
        var executionId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ScheduleExecutionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(executionId));

        var request = new
        {
            sheetId = Guid.NewGuid(),
            workerId = Guid.NewGuid(),
            enrollmentId = Guid.NewGuid(),
            machineId = "CNC-01",
            scheduleStart = DateTime.UtcNow,
            scheduleEnd = DateTime.UtcNow.AddHours(1),
            totalPanels = 5
        };

        var response = await _client.PostAsJsonAsync("/api/cutting/executions/", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_Executions_WithValidationError_Returns422()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ScheduleExecutionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Invalid(new ValidationError("SheetId must not be empty.")));

        var request = new
        {
            sheetId = Guid.Empty,
            workerId = Guid.NewGuid(),
            enrollmentId = Guid.NewGuid(),
            machineId = "CNC-01",
            scheduleStart = DateTime.UtcNow,
            scheduleEnd = DateTime.UtcNow.AddHours(1),
            totalPanels = 5
        };

        var response = await _client.PostAsJsonAsync("/api/cutting/executions/", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_Executions_WithScheduleCommand_ReturnsMediatorResult()
    {
        // The TestServer auth handler always authenticates — verify 201 is returned
        var executionId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ScheduleExecutionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(executionId));

        var request = new
        {
            sheetId = Guid.NewGuid(),
            workerId = Guid.NewGuid(),
            enrollmentId = Guid.NewGuid(),
            machineId = "CNC-02",
            scheduleStart = DateTime.UtcNow,
            scheduleEnd = DateTime.UtcNow.AddHours(2),
            totalPanels = 8
        };

        var response = await _client.PostAsJsonAsync("/api/cutting/executions/", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Dispose() => _client.Dispose();

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim("tid", TenantId.ToString()),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
