using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Application.Services;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Audit;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Crypto;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.HashChain;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Inventory;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Realtime;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Security;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.StageRegistry;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Extensions;

/// <summary>
/// Registers all Phase 4 Execution infrastructure services into the DI container.
/// Call this from <c>Program.cs</c> after <c>AddCuttingInfrastructure</c>.
/// </summary>
public static class CuttingExecutionInfrastructureExtensions
{
    /// <summary>
    /// Adds Phase 4 Execution infrastructure: repository, crypto, audit, stubs, and SignalR publisher.
    /// </summary>
    public static IServiceCollection AddCuttingExecutionInfrastructure(
        this IServiceCollection services)
    {
        // Outbox interceptor — singleton, registered into existing CuttingDbContext via AddCuttingInfrastructure
        services.AddSingleton<OutboxSaveChangesInterceptor>();

        // Execution repository
        services.AddScoped<ICuttingExecutionRepository, CuttingExecutionRepository>();

        // Consent withdrawal repository (in-memory for now)
        services.AddSingleton<IConsentWithdrawalRepository, InMemoryConsentWithdrawalRepository>();

        // Crypto
        services.AddSingleton<IPerExecutionKeyVault, PerExecutionKeyVault>();
        services.AddSingleton<TwoSlotMasterKekProvider>();

        // Port implementations
        services.AddScoped<ICuttingAuditLogger, SerilogCuttingAuditLogger>();
        services.AddScoped<ICuttingHashChainSink, NullCuttingHashChainSink>();
        services.AddScoped<IStageRegistry, NullStageRegistry>();
        services.AddScoped<IOffcutNotificationSink, NullOffcutNotificationSink>();

        // SignalR real-time publisher
        services.AddScoped<ICuttingExecutionRealtimePublisher, SignalRCuttingExecutionRealtimePublisher>();

        // Security policies (stub implementations for dev/test)
        services.AddScoped<IWorkerSecurityPolicy, NullWorkerSecurityPolicy>();
        services.AddScoped<ICuttingProofPolicy, NullCuttingProofPolicy>();

        // Application services
        services.AddScoped<PredicateFactoryV1>();

        return services;
    }
}
