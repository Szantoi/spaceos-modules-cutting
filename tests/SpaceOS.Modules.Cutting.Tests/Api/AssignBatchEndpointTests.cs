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
using SpaceOS.Modules.Cutting.Application.Commands.AssignBatch;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class AssignBatchEndpointTests
{
    private HttpClient CreateAuthClient(Mock<IMediator> mediatorMock, Mock<ICuttingRepository>? repoMock = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddSingleton((repoMock ?? new Mock<ICuttingRepository>()).Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, MachineOperatorAuthHandler>("Test", _ => { });
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

    /// <summary>Auth handler that provides machine_operator role for assign-batch tests.</summary>
    private sealed class MachineOperatorAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public MachineOperatorAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("tid", Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "machine_operator"),
                new Claim(ClaimTypes.Role, "Manufacturer")
            };
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    [Fact]
    public async Task AssignBatch_ValidRequest_ReturnsExecutionId()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var executionId = Guid.NewGuid();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssignBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AssignBatchResponse>.Success(
                new AssignBatchResponse(executionId, "Planned")));

        var client = CreateAuthClient(mediatorMock);
        var payload = new
        {
            batchId = Guid.NewGuid(),
            machineId = Guid.NewGuid(),
            operatorId = Guid.NewGuid(),
            priority = 5,
            startTime = DateTime.UtcNow.AddHours(1).ToString("o")
        };

        // Act
        var response = await client.PostAsJsonAsync("/cutting/api/plans/2026-06-17/assign-batch", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AssignBatchResponseDto>();
        content.Should().NotBeNull();
        content!.ExecutionId.Should().Be(executionId);
        content.Status.Should().Be("Planned");
    }

    [Fact]
    public async Task AssignBatch_InvalidRole_Returns403()
    {
        // Arrange: User authenticated but without machine_operator or production_manager role
        var mediatorMock = new Mock<IMediator>();
        var repoMock = new Mock<ICuttingRepository>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddSingleton(repoMock.Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, RegularUserAuthHandler>("Test", _ => { });
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
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        var payload = new
        {
            batchId = Guid.NewGuid(),
            machineId = Guid.NewGuid(),
            operatorId = Guid.NewGuid(),
            priority = 5,
            startTime = DateTime.UtcNow.AddHours(1).ToString("o")
        };

        // Act
        var response = await client.PostAsJsonAsync("/cutting/api/plans/2026-06-17/assign-batch", payload);

        // Assert: Returns 403 Forbidden for user without required role
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>Auth handler for regular user without machine_operator/production_manager role.</summary>
    private sealed class RegularUserAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public RegularUserAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, "regularuser"),
                new Claim("tid", Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "viewer") // Not machine_operator or production_manager
            };
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    [Fact]
    public async Task AssignBatch_DuplicateBatch_Returns409()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var batchId = Guid.NewGuid();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssignBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AssignBatchResponse>.Conflict(
                $"Batch {batchId} is already assigned for 2026-06-17."));

        var client = CreateAuthClient(mediatorMock);
        var payload = new
        {
            batchId,
            machineId = Guid.NewGuid(),
            operatorId = Guid.NewGuid(),
            priority = 5,
            startTime = DateTime.UtcNow.AddHours(1).ToString("o")
        };

        // Act
        var response = await client.PostAsJsonAsync("/cutting/api/plans/2026-06-17/assign-batch", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AssignBatch_InvalidBatchId_Returns400()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AssignBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AssignBatchResponse>.Invalid(
                new ValidationError("Batch not found.")));

        var client = CreateAuthClient(mediatorMock);
        var payload = new
        {
            batchId = Guid.NewGuid(),
            machineId = Guid.NewGuid(),
            operatorId = Guid.NewGuid(),
            priority = 5,
            startTime = DateTime.UtcNow.AddHours(1).ToString("o")
        };

        // Act
        var response = await client.PostAsJsonAsync("/cutting/api/plans/2026-06-17/assign-batch", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignBatch_InvalidDateFormat_Returns400()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var client = CreateAuthClient(mediatorMock);
        var payload = new
        {
            batchId = Guid.NewGuid(),
            machineId = Guid.NewGuid(),
            operatorId = Guid.NewGuid(),
            priority = 5,
            startTime = DateTime.UtcNow.AddHours(1).ToString("o")
        };

        // Act
        var response = await client.PostAsJsonAsync("/cutting/api/plans/not-a-date/assign-batch", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record AssignBatchResponseDto(Guid ExecutionId, string Status);
}
