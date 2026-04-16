using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;

namespace SpaceOS.Modules.Cutting.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCuttingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CuttingDbContext>(options =>
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_cutting")));

        services.AddScoped<ICuttingRepository, CuttingRepository>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICuttingTenantAccessor, HttpContextCuttingTenantAccessor>();
        services.AddScoped<ICuttingProvider, CuttingProviderAdapter>();
        services.AddSingleton<NestingService>();

        return services;
    }
}
