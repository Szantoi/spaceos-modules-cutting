using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.Commands.SubmitCuttingSheet;

namespace SpaceOS.Modules.Cutting.Api.Extensions;

public static class CuttingApplicationExtensions
{
    public static IServiceCollection AddCuttingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(SubmitCuttingSheetCommandHandler).Assembly));
        return services;
    }
}
