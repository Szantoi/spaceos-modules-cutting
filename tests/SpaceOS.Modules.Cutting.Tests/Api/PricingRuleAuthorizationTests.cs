using System.Net;
using System.Net.Http.Headers;
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
using SpaceOS.Modules.Cutting.Application.Queries.GetPricingRule;
using SpaceOS.Modules.Cutting.Api.Endpoints;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

public class PricingRuleAuthorizationTests
{
    [Fact]
    public async Task GetPricingRule_Anonymous_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/pricing-rules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPricingRule_AuthenticatedNonManufacturer_Returns403()
    {
        var client = CreateClient("viewer");

        var response = await client.GetAsync($"/api/pricing-rules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPricingRule_Manufacturer_Returns200()
    {
        var client = CreateClient("manufacturer");

        var response = await client.GetAsync($"/api/pricing-rules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpClient CreateClient(string? profile = null)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPricingRuleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PricingRuleDto>.Success(new PricingRuleDto
            {
                Id = Guid.NewGuid(),
                ProductCategory = "test",
                Status = "Draft"
            }));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediator.Object);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, PricingPolicyAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(options =>
            options.AddPolicy("ManufacturerOnly", policy =>
                policy.RequireClaim("tenant_type", "Manufacturer")));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPricingRuleEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var server = app.Services.GetRequiredService<IServer>() as TestServer;
        var client = server!.CreateClient();
        if (profile is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", profile);

        return client;
    }

    private sealed class PricingPolicyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public PricingPolicyAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var profile = Request.Headers.Authorization.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();

            if (profile is null)
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new("tid", Guid.NewGuid().ToString())
            };

            if (string.Equals(profile, "manufacturer", StringComparison.Ordinal))
                claims.Add(new Claim("tenant_type", "Manufacturer"));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
