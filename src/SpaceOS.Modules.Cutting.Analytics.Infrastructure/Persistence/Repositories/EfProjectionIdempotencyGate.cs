using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectionIdempotencyGate"/>.
/// Uses an atomic INSERT … ON CONFLICT DO NOTHING to detect duplicates.
/// The caller is responsible for wrapping the projection in a transaction (BE-07 pattern).
/// </summary>
public sealed class EfProjectionIdempotencyGate(
    CuttingAnalyticsDbContext db,
    ILogger<EfProjectionIdempotencyGate> logger)
    : IProjectionIdempotencyGate
{
    /// <inheritdoc/>
    public async Task<bool> IsAlreadyProcessedAsync(
        Guid eventId, string subscriberName, Guid tenantId, CancellationToken ct)
    {
        // ExecuteSqlInterpolatedAsync goes through the EF interceptor pipeline (BE-01).
        var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO cutting_analytics.""ProcessedOutboxEvents""
                   (""EventId"", ""SubscriberName"", ""TenantId"", ""CreatedAt"")
               VALUES ({eventId}, {subscriberName}, {tenantId}, NOW())
               ON CONFLICT (""EventId"", ""SubscriberName"") DO NOTHING",
            ct).ConfigureAwait(false);

        var alreadyProcessed = rowsAffected == 0;
        if (alreadyProcessed)
            logger.LogDebug(
                "Duplicate event {EventId} for subscriber {Subscriber} — skipping projection.",
                eventId, subscriberName);

        return alreadyProcessed;
    }
}
