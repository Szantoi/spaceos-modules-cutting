using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Background;

/// <summary>
/// BE-04: Injects <see cref="IServiceScopeFactory"/> to create scoped services safely.
/// BE-07: Uses a bounded <see cref="Channel{T}"/> to decouple producers from consumers.
/// BE-09: Injects <see cref="TimeProvider"/> for testable time operations.
/// SEC-10: Uses a <see cref="SemaphoreSlim"/> to limit concurrent tenant polling to 10.
/// </summary>
internal sealed class PollSchedulerBackgroundService : BackgroundService
{
    private const int MaxConcurrentTenants = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<PollSchedulerBackgroundService> _logger;

    // BE-07: bounded channel — prevents unbounded memory growth
    private readonly Channel<Guid> _pollQueue =
        Channel.CreateBounded<Guid>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });

    // SEC-10: per-cluster concurrency limit
    private readonly SemaphoreSlim _concurrencyLimiter = new(MaxConcurrentTenants, MaxConcurrentTenants);

    public PollSchedulerBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILogger<PollSchedulerBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PollSchedulerBackgroundService started.");

        var timerTask = RunTimerLoopAsync(stoppingToken);
        var workerTask = RunWorkerLoopAsync(stoppingToken);

        try
        {
            await Task.WhenAll(timerTask, workerTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PollSchedulerBackgroundService stopping.");
        }
    }

    private async Task RunTimerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
                _logger.LogDebug("Poll tick at {Time}.", _clock.GetUtcNow());
                // In a full implementation, load pending tenants from a registry and enqueue them.
                // Stub: no tenants registered yet.
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunWorkerLoopAsync(CancellationToken ct)
    {
        await foreach (var tenantId in _pollQueue.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            await _concurrencyLimiter.WaitAsync(ct).ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                try
                {
                    await PollTenantAsync(tenantId, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Polling failed for tenant {TenantId}.", tenantId);
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }, ct);
        }
    }

    private async Task PollTenantAsync(Guid tenantId, CancellationToken ct)
    {
        // BE-04: use scope for all scoped services
        using var scope = _scopeFactory.CreateScope();
        _logger.LogDebug("Polling adapter results for tenant {TenantId}.", tenantId);
        await Task.Yield(); // placeholder — full implementation calls transport PollResultAsync
    }

    public override void Dispose()
    {
        _concurrencyLimiter.Dispose();
        base.Dispose();
    }
}
