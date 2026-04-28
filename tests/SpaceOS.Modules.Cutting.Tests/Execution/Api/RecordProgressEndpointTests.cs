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
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Api;

public class RecordProgressEndpointTests : IDisposable
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly HttpClient _client;
    private readonly Mock<IMediator> _mediatorMock = new();

    public RecordProgressEndpointTests()
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
    public async Task PostProgress_WithValidRequest_Returns200()
    {
        var executionId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RecordProgressCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var request = new
        {
            eventId = Guid.NewGuid(),
            kind = ProgressEventKind.PanelCompleted,
            panel = 1,
            occurredAt = DateTime.UtcNow,
            eventHmacBase64 = Convert.ToBase64String(new byte[32]),
            hmacKeyVersion = "v1"
        };

        var response = await _client.PostAsJsonAsync($"/api/cutting/executions/{executionId}/progress", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostProgress_NotFound_Returns404()
    {
        var executionId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RecordProgressCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.NotFound("Execution not found."));

        var request = new
        {
            eventId = Guid.NewGuid(),
            kind = ProgressEventKind.MachineEvent,
            panel = (int?)null,
            occurredAt = DateTime.UtcNow,
            eventHmacBase64 = Convert.ToBase64String(new byte[32]),
            hmacKeyVersion = "v1"
        };

        var response = await _client.PostAsJsonAsync($"/api/cutting/executions/{executionId}/progress", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostProgress_ValidationError_Returns422()
    {
        var executionId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RecordProgressCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Invalid(new ValidationError("HMAC validation failed.")));

        var request = new
        {
            eventId = Guid.NewGuid(),
            kind = ProgressEventKind.PanelCompleted,
            panel = 1,
            occurredAt = DateTime.UtcNow,
            eventHmacBase64 = Convert.ToBase64String(new byte[32]),
            hmacKeyVersion = "v1"
        };

        var response = await _client.PostAsJsonAsync($"/api/cutting/executions/{executionId}/progress", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
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
