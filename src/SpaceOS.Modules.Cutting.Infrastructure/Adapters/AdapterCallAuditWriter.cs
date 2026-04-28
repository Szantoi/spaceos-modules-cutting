using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// SEC-08: All error_message fields are sanitized (control chars stripped, truncated to 8000 chars)
/// before being written to the audit table.
/// </summary>
internal sealed class AdapterCallAuditWriter : IAdapterCallAuditWriter
{
    private readonly CuttingDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ILogger<AdapterCallAuditWriter> _logger;

    public AdapterCallAuditWriter(
        CuttingDbContext db,
        TimeProvider clock,
        ILogger<AdapterCallAuditWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task RecordSubmitStartedAsync(
        Guid callId, string adapterName, string methodName, Guid tenantId, CancellationToken ct)
    {
        var entity = new AdapterCallAuditEntity
        {
            CallId = callId,
            TenantId = tenantId,
            AdapterName = adapterName,
            TransportName = "unknown",
            MethodName = methodName,
            StartedAt = _clock.GetUtcNow(),
            Status = "started"
        };
        _db.AdapterCallAudits.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordSubmitCompletedAsync(Guid callId, string correlationId, CancellationToken ct)
    {
        var entity = await _db.AdapterCallAudits
            .FindAsync(new object[] { callId, _clock.GetUtcNow() }, ct)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            entity.Status = "completed";
            entity.CorrelationId = correlationId;
            entity.CompletedAt = _clock.GetUtcNow();
        }
        else
        {
            // Insert a synthetic completion row — happens when started row was in different partition
            _db.AdapterCallAudits.Add(new AdapterCallAuditEntity
            {
                CallId = callId,
                TenantId = Guid.Empty,
                AdapterName = "unknown",
                TransportName = "unknown",
                MethodName = "completed",
                CorrelationId = correlationId,
                StartedAt = _clock.GetUtcNow(),
                CompletedAt = _clock.GetUtcNow(),
                Status = "completed"
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordFailureAsync(Guid callId, IEnumerable<string> errors, CancellationToken ct)
    {
        var sanitized = AuditSanitizer.Sanitize(string.Join("; ", errors));
        _db.AdapterCallAudits.Add(new AdapterCallAuditEntity
        {
            CallId = callId,
            TenantId = Guid.Empty,
            AdapterName = "unknown",
            TransportName = "unknown",
            MethodName = "failure",
            StartedAt = _clock.GetUtcNow(),
            CompletedAt = _clock.GetUtcNow(),
            Status = "failed",
            ErrorMessage = sanitized
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordExceptionAsync(Guid callId, Exception ex, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ex);
        var sanitized = AuditSanitizer.Sanitize(ex.Message);
        _db.AdapterCallAudits.Add(new AdapterCallAuditEntity
        {
            CallId = callId,
            TenantId = Guid.Empty,
            AdapterName = "unknown",
            TransportName = "unknown",
            MethodName = "exception",
            StartedAt = _clock.GetUtcNow(),
            CompletedAt = _clock.GetUtcNow(),
            Status = "exception",
            ErrorMessage = sanitized
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordCapabilityFallbackAsync(
        string providerName, string capability, Guid tenantId, CancellationToken ct)
    {
        _logger.LogWarning(
            "Capability fallback to builtin: provider={Provider}, capability={Capability}, tenant={TenantId}",
            providerName, capability, tenantId);

        _db.AdapterCallAudits.Add(new AdapterCallAuditEntity
        {
            CallId = Guid.NewGuid(),
            TenantId = tenantId,
            AdapterName = providerName,
            TransportName = "none",
            MethodName = "capability-fallback",
            StartedAt = _clock.GetUtcNow(),
            CompletedAt = _clock.GetUtcNow(),
            Status = "fallback",
            ErrorMessage = AuditSanitizer.Sanitize($"Capability '{capability}' not available on '{providerName}'")
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
