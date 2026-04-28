using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Polls for pending outbox events and dispatches them to the registered projectors.
/// Uses a per-batch scope so each iteration gets a fresh <c>DbContext</c> (BE-A02 pattern).
/// </summary>
public sealed class AnalyticsProjectionSubscriber(
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsProjectionSubscriber> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Per-batch scope: fresh DbContext each iteration.
                await using var scope = scopeFactory.CreateAsyncScope();
                var executionProjector = scope.ServiceProvider.GetRequiredService<IExecutionMetricProjector>();
                var materialProjector = scope.ServiceProvider.GetRequiredService<IMaterialUsageProjector>();

                // Real implementation would poll the outbox table and dispatch events.
                // Stub: outbox not wired in this environment — projectors are registered.
                _ = executionProjector;
                _ = materialProjector;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {Service} — will retry in {Interval}.",
                    nameof(AnalyticsProjectionSubscriber), PollInterval);
            }

            try
            {
                await Task.Delay(PollInterval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
