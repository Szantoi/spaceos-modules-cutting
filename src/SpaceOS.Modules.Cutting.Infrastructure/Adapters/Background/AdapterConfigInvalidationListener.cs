using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Background;

/// <summary>
/// BE-04: Listens for adapter configuration change events and invalidates the distributed cache entry.
/// Injects <see cref="IServiceScopeFactory"/> (not <see cref="IDistributedCache"/> directly) to
/// safely create scoped services from a hosted background service.
/// </summary>
internal sealed class AdapterConfigInvalidationListener : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdapterConfigInvalidationListener> _logger;

    public AdapterConfigInvalidationListener(
        IServiceScopeFactory scopeFactory,
        ILogger<AdapterConfigInvalidationListener> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // NOTE: This implementation is a stub.
        // A full implementation would subscribe to a Redis pub/sub channel
        // "adapter-config-changed:*" using StackExchange.Redis.ISubscriber.
        // When a message arrives, it creates a scope, resolves IDistributedCache,
        // and calls RemoveAsync("adapter-config:{tenantId}").
        //
        // Stub: log that the listener is running and wait for cancellation.
        _logger.LogInformation(
            "AdapterConfigInvalidationListener started. Waiting for config-change events.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AdapterConfigInvalidationListener stopping.");
        }
    }

    /// <summary>
    /// Invalidates the distributed cache entry for the given tenant's adapter config.
    /// Called by the message handler when a config-changed event is received.
    /// </summary>
    internal async Task InvalidateCacheAsync(Guid tenantId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var key = $"adapter-config:{tenantId}";
        await cache.RemoveAsync(key, ct).ConfigureAwait(false);
        _logger.LogInformation("Invalidated adapter config cache for tenant {TenantId}.", tenantId);
    }
}
