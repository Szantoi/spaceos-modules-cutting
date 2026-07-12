using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for QuoteRequest integration tests.
/// Resolves DI scope issues by providing test-specific service configurations.
/// </summary>
public class CuttingWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing CuttingDbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CuttingDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove the existing IDbContextFactory registration (causes DI scope conflict)
            var dbFactoryDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextFactory<CuttingDbContext>));
            if (dbFactoryDescriptor != null)
            {
                services.Remove(dbFactoryDescriptor);
            }

            // Remove the existing ITenantResolver registration
            var tenantResolverDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantResolver));
            if (tenantResolverDescriptor != null)
            {
                services.Remove(tenantResolverDescriptor);
            }

            // Remove the existing IEmailService registration
            var emailServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<CuttingDbContext>(options =>
            {
                options.UseInMemoryDatabase("CuttingTestDb");
            });

            // Mock TenantResolver to return a fixed tenant ID
            services.AddScoped<ITenantResolver, MockTenantResolver>();

            // Mock EmailService (no-op for tests)
            services.AddScoped<IEmailService, MockEmailService>();

            // Configure test authentication (added after JwtBearer, will override default scheme)
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                // Override default scheme to use Test authentication
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
            });

            // Add Test authentication scheme
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    options => { });

            // Override authorization to always succeed in tests
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ManufacturerOnly", policy => policy.RequireAssertion(_ => true));
            });
        });
    }
}

/// <summary>
/// Mock implementation of ITenantResolver for integration tests.
/// Always returns a fixed tenant ID.
/// </summary>
internal class MockTenantResolver : ITenantResolver
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<Guid> GetTenantIdFromSubdomain(string hostname, CancellationToken ct)
    {
        // Return a fixed test tenant ID
        return Task.FromResult(TestTenantId);
    }
}

/// <summary>
/// Mock implementation of IEmailService for integration tests.
/// No-op implementation that doesn't send actual emails.
/// </summary>
internal class MockEmailService : IEmailService
{
    public Task SendQuoteRequestNotification(
        string customerEmail,
        string adminEmail,
        string quoteNumber,
        string trackingToken,
        string trackingUrl,
        CancellationToken ct)
    {
        // No-op for tests
        return Task.CompletedTask;
    }

    public Task SendQuoteApprovedNotification(
        string customerEmail,
        string quoteNumber,
        decimal price,
        string currency,
        string acceptUrl,
        CancellationToken ct)
    {
        // No-op for tests
        return Task.CompletedTask;
    }

    public Task SendQuoteRejectedNotification(
        string customerEmail,
        string quoteNumber,
        string? reason,
        CancellationToken ct)
    {
        // No-op for tests
        return Task.CompletedTask;
    }
}
