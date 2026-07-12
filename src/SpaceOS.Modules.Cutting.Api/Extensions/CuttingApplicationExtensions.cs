using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.Behaviors;
using SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;

namespace SpaceOS.Modules.Cutting.Api.Extensions;

public static class CuttingApplicationExtensions
{
    public static IServiceCollection AddCuttingApplication(this IServiceCollection services)
    {
        // Register MediatR handlers from both Application assemblies
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(SubmitCuttingSheetCommandHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(RecordOffcutCommand).Assembly); // Execution.Application
        });

        // Register FluentValidation validators from both assemblies
        services.AddValidatorsFromAssembly(typeof(SubmitCuttingSheetCommandHandler).Assembly);
        services.AddValidatorsFromAssembly(typeof(RecordOffcutCommand).Assembly); // Execution.Application

        // Register MediatR pipeline behaviors (order matters: Logging → Validation → Handler)
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Strategy factory — scoped is safe; strategies themselves are stateless
        services.AddScoped<IPlanningStrategyFactory, PlanningStrategyFactory>();

        // Default strategy (maxcut-v1) available for direct injection where needed
        services.AddScoped<IPlanningStrategy>(sp =>
            sp.GetRequiredService<IPlanningStrategyFactory>().GetStrategy("maxcut-v1"));

        return services;
    }
}
