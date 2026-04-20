using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;
using SpaceOS.Modules.Cutting.Application.Strategies;

namespace SpaceOS.Modules.Cutting.Api.Extensions;

public static class CuttingApplicationExtensions
{
    public static IServiceCollection AddCuttingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(SubmitCuttingSheetCommandHandler).Assembly));

        // Strategy factory — scoped is safe; strategies themselves are stateless
        services.AddScoped<IPlanningStrategyFactory, PlanningStrategyFactory>();

        // Default strategy (maxcut-v1) available for direct injection where needed
        services.AddScoped<IPlanningStrategy>(sp =>
            sp.GetRequiredService<IPlanningStrategyFactory>().GetStrategy("maxcut-v1"));

        return services;
    }
}
