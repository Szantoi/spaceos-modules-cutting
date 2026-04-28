using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.BackgroundServices;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Tpm;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering the Analytics Infrastructure layer with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="CuttingAnalyticsDbContext"/>, all repository implementations,
    /// background services, rate limiter, and TPM stub.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">PostgreSQL connection string targeting the analytics DB.</param>
    public static IServiceCollection AddCuttingAnalyticsInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string required.", nameof(connectionString));

        services.AddDbContext<CuttingAnalyticsDbContext>(opts =>
            opts.UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "cutting_analytics")));

        services.AddScoped<IAnalyticsQueryRepository, EfAnalyticsQueryRepository>();
        services.AddScoped<IRebuildJobRepository, EfRebuildJobRepository>();
        services.AddScoped<IProjectionIdempotencyGate, EfProjectionIdempotencyGate>();

        services.AddSingleton<RedisSentinelRateLimiter>();
        services.AddSingleton<IRateLimiter>(sp => sp.GetRequiredService<RedisSentinelRateLimiter>());
        services.AddSingleton<IHandshakeRateLimiter>(sp => sp.GetRequiredService<RedisSentinelRateLimiter>());

        services.AddSingleton(TpmFallbackPolicy.Default);
        services.AddSingleton<ITpmKeyProvisioner, KekFallbackProvisioner>();

        services.AddHostedService<AnalyticsProjectionSubscriber>();
        services.AddHostedService<RebuildBackgroundService>();
        services.AddHostedService<ProcessedEventRetentionWorker>();

        return services;
    }
}
