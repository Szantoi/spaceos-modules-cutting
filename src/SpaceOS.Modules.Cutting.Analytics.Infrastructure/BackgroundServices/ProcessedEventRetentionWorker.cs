using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Periodically purges <c>ProcessedOutboxEvents</c> older than 90 days to bound table growth.
/// Runs once every 24 hours.
/// </summary>
public sealed class ProcessedEventRetentionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessedEventRetentionWorker> logger)
    : BackgroundService
{
    internal static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);
    internal static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<CuttingAnalyticsDbContext>();
                var cutoff = DateTime.UtcNow - RetentionPeriod;

                var deleted = await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"DELETE FROM cutting_analytics.""ProcessedOutboxEvents"" WHERE ""CreatedAt"" < {cutoff}",
                    ct).ConfigureAwait(false);

                if (deleted > 0)
                    logger.LogInformation(
                        "Retention worker deleted {Count} expired dedup record(s) older than {Cutoff:O}.",
                        deleted, cutoff);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful shutdown — stop the loop.
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {Service} — will retry in {Interval}.",
                    nameof(ProcessedEventRetentionWorker), RunInterval);
            }

            try
            {
                await Task.Delay(RunInterval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
