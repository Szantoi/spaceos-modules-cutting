using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using SpaceOS.Modules.Inventory.Contracts.Providers;

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

        services.AddDbContext<CuttingDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_cutting"));
            options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
        });

        services.AddScoped<ICuttingRepository, CuttingRepository>();
        services.AddScoped<ICuttingTenantAccessor, HttpContextCuttingTenantAccessor>();
        services.AddScoped<ICuttingProvider, CuttingProviderAdapter>();
        services.AddSingleton<NestingService>();

        services.AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>(client =>
        {
            client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"] ?? "http://127.0.0.1:5004");
        });

        return services;
    }
}
