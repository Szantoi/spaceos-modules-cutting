using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.Events;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Nesting.Algorithms;
using SpaceOS.Nesting.Algorithms.Strategies;
using SpaceOS.Modules.Cutting.Infrastructure.Events;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
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

        services.AddScoped<ICuttingRepository, CuttingRepository>();
        services.AddScoped<IPriorityProfileRepository, PriorityProfileRepository>();
        services.AddScoped<IPanelReservationRepository, PanelReservationRepository>();
        services.AddScoped<IPlanNestingSnapshotRepository, PlanNestingSnapshotRepository>();
        services.AddScoped<ICuttingTenantAccessor, HttpContextCuttingTenantAccessor>();
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

        return services;
    }
}
