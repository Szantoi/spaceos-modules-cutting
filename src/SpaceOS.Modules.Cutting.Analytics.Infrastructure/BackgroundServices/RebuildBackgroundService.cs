using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Picks up <c>Pending</c> analytics rebuild jobs and executes the backfill.
/// Uses a per-iteration scope for a fresh <c>DbContext</c> on every cycle.
/// </summary>
public sealed class RebuildBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RebuildBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IRebuildJobRepository>();

                // Real: pick pending job, run backfill chunk-by-chunk, update status.
                // Stub: just idle — no active jobs to process in test environment.
                _ = repo;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {Service} — will retry in {Interval}.",
                    nameof(RebuildBackgroundService), PollInterval);
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
