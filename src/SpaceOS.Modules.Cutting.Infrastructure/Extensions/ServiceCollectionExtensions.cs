using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Application.Events;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Nesting.Algorithms;
using SpaceOS.Nesting.Algorithms.Strategies;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Background;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.FileSystem;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Resilience;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using SpaceOS.Modules.Cutting.Infrastructure.Events;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using SpaceOS.Modules.Cutting.Infrastructure.Workers;
using OldInventoryProvider = SpaceOS.Modules.Inventory.Contracts.Providers.IInventoryProvider;

namespace SpaceOS.Modules.Cutting.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCuttingInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<TenantSessionInterceptor>();
        services.AddSingleton<OutboxSaveChangesInterceptor>();

        services.AddDbContext<CuttingDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_cutting"));
            options.AddInterceptors(
                sp.GetRequiredService<TenantSessionInterceptor>(),
                sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });

        // Register DbContext factory (required for TenantResolver cross-schema queries)
        // NOTE: Factory is registered as Singleton with explicit options to avoid scoped dependency issues
        services.AddDbContextFactory<CuttingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_cutting"));
            // Interceptors NOT added here - they are scoped and would cause lifetime conflicts
        });

        services.AddScoped<ICuttingRepository, CuttingRepository>();
        services.AddScoped<IPriorityProfileRepository, PriorityProfileRepository>();
        services.AddScoped<IPanelReservationRepository, PanelReservationRepository>();
        services.AddScoped<IPlanNestingSnapshotRepository, PlanNestingSnapshotRepository>();
        services.AddScoped<IQuoteRequestRepository, QuoteRequestRepository>();
        services.AddScoped<IQuoteNotificationService, SpaceOS.Modules.Cutting.Infrastructure.Services.QuoteNotificationService>();
        services.AddScoped<ICuttingTenantAccessor, HttpContextCuttingTenantAccessor>();

        // Q3 Track A: Tenant resolver + Email service
        services.AddScoped<ITenantResolver, SpaceOS.Modules.Cutting.Infrastructure.Services.TenantResolver>();
        services.AddScoped<IEmailService, SpaceOS.Modules.Cutting.Infrastructure.Services.EmailService>();
        services.AddScoped<ICuttingProvider, CuttingProviderAdapter>();
        services.AddSingleton<INestingStrategy, FfdhNestingStrategy>();
        services.AddSingleton<NestingStrategyFactory>();
        services.AddSingleton<ICapacityModel, AreaCapacityModel>();
        services.AddSingleton<IReworkPolicy, WarnAndApplyPolicy>();
        services.AddScoped<PanelSourceService>();

        services.AddHttpClient<OldInventoryProvider, InventoryProviderHttpAdapter>("InventoryProvider.Legacy", client =>
        {
            client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"] ?? "http://127.0.0.1:5004");
        });

        services.AddHttpClient<SpaceOS.Modules.Contracts.Inventory.IInventoryProvider, ContractsInventoryHttpAdapter>("InventoryProvider.Contracts", client =>
        {
            client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"] ?? "http://127.0.0.1:5004");
        });

        services.AddHttpClient<ICuttingEventPublisher, CuttingEventPublisher>(client =>
        {
            client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"] ?? "http://127.0.0.1:5004");
        });

        services.AddHttpClient<IInventoryCuttingAdapter, InventoryCuttingHttpAdapter>(client =>
        {
            client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"] ?? "http://127.0.0.1:5004");
        });

        services.AddHostedService<DaySlotAutoLockWorker>();

        // ── Adapter framework ──────────────────────────────────────────────
        // Distributed cache (in-memory fallback; production uses Redis)
        services.AddDistributedMemoryCache();

        // Infrastructure utilities
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBoundedSubprocessRunner, BoundedSubprocessRunner>();
        services.AddScoped<ITenantAdapterStorage, TenantAdapterStorage>();

        // Transports
        services.AddSingleton<IIpRangeChecker, IpRangeChecker>();
        services.AddScoped<FileExchangeTransport>();
        services.AddScoped<CliWrapperTransport>();
        services.AddScoped<RestApiTransport>();

        // Format converters
        services.AddScoped<OptiCutFormatConverter>();
        services.AddScoped<CutRiteFormatConverter>();

        // Audit + repositories
        services.AddScoped<IAdapterCallAuditWriter, AdapterCallAuditWriter>();
        services.AddScoped<ITenantCuttingProviderConfigRepository, TenantCuttingProviderConfigRepository>();
        services.AddScoped<IAdapterHealthRecordRepository, AdapterHealthRecordRepository>();

        // Secret detector (ConfigSecretDetector is in Application layer)
        services.AddSingleton<IConfigSecretDetector, SpaceOS.Modules.Cutting.Application.Adapters.ConfigSecretDetector>();

        // Named provider implementations
        services.AddScoped<CuttingProviderAdapter>();
        services.AddScoped<BuiltinCuttingProvider>();
        services.AddScoped<OptiCutAdapter>(sp => new OptiCutAdapter(
            sp.GetRequiredService<FileExchangeTransport>(),
            sp.GetRequiredService<OptiCutFormatConverter>(),
            sp.GetRequiredService<IAdapterCallAuditWriter>(),
            sp.GetRequiredService<ICuttingTenantAccessor>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OptiCutAdapter>>()));
        services.AddScoped<CutRiteAdapter>(sp => new CutRiteAdapter(
            sp.GetRequiredService<CliWrapperTransport>(),
            sp.GetRequiredService<CutRiteFormatConverter>(),
            sp.GetRequiredService<IAdapterCallAuditWriter>(),
            sp.GetRequiredService<ICuttingTenantAccessor>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CutRiteAdapter>>()));
        services.AddScoped<ManualAdapter>();

        // Adapter factory — creates one per scope with all four named providers
        services.AddScoped<IAdapterFactory>(sp => new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", sp.GetRequiredService<BuiltinCuttingProvider>()),
            new KeyedAdapterRegistration("opticut", sp.GetRequiredService<OptiCutAdapter>()),
            new KeyedAdapterRegistration("cutrite", sp.GetRequiredService<CutRiteAdapter>()),
            new KeyedAdapterRegistration("manual", sp.GetRequiredService<ManualAdapter>()),
        }));

        // Provider resolver (reads tenant config → returns correct provider)
        services.AddScoped<ICuttingProviderResolver>(sp =>
        {
            var tenantAccessor = sp.GetRequiredService<ICuttingTenantAccessor>();
            return new CuttingProviderResolver(
                sp.GetRequiredService<IAdapterFactory>(),
                sp.GetRequiredService<ITenantCuttingProviderConfigRepository>(),
                sp.GetRequiredService<IDistributedCache>(),
                sp.GetRequiredService<IAdapterCallAuditWriter>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CuttingProviderResolver>>(),
                () => tenantAccessor.TenantId);
        });

        // Background services
        services.AddHostedService<AdapterConfigInvalidationListener>();
        services.AddHostedService<PollSchedulerBackgroundService>();

        return services;
    }
}
