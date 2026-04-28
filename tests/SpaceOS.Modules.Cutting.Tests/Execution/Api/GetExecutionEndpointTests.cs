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
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.GetExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Queries.ListExecutions;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Api;

public class GetExecutionEndpointTests : IDisposable
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly HttpClient _client;
    private readonly Mock<IMediator> _mediatorMock = new();

    public GetExecutionEndpointTests()
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
    public async Task Get_Execution_Found_Returns200()
    {
        var executionId = Guid.NewGuid();
        var dto = new ExecutionDto(executionId, TenantId, Guid.NewGuid(), "Scheduled", 0, 5, null, null);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetExecutionQuery>(q => q.ExecutionId == executionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExecutionDto>.Success(dto));

        var response = await _client.GetAsync($"/api/cutting/executions/{executionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Execution_NotFound_Returns404()
    {
        var executionId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetExecutionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExecutionDto>.NotFound("Not found."));

        var response = await _client.GetAsync($"/api/cutting/executions/{executionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Executions_List_Returns200()
    {
        var summaries = new List<ExecutionSummaryDto>
        {
            new(Guid.NewGuid(), "Scheduled", DateTime.UtcNow, 0, 5)
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ListExecutionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ExecutionSummaryDto>>.Success(summaries));

        var response = await _client.GetAsync("/api/cutting/executions/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Executions_EmptyList_Returns200()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ListExecutionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ExecutionSummaryDto>>.Success(Array.Empty<ExecutionSummaryDto>()));

        var response = await _client.GetAsync("/api/cutting/executions/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
