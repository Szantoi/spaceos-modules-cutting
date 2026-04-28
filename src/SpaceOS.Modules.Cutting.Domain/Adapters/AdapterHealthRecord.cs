using SpaceOS.Modules.Cutting.Domain.Adapters.Events;
using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Adapters;

/// <summary>
/// Aggregate root tracking health check state for a specific (TenantId, AdapterName) pair.
/// </summary>
public sealed class AdapterHealthRecord : AggregateRoot
{
    /// <summary>Tenant identifier — part of composite primary key.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Adapter name — part of composite primary key.</summary>
    public string AdapterName { get; private set; } = string.Empty;

    /// <summary>Timestamp of the most recent health check attempt.</summary>
    public DateTimeOffset LastCheckAt { get; private set; }

    /// <summary>Timestamp of the most recent successful health check, if any.</summary>
    public DateTimeOffset? LastSuccessAt { get; private set; }

    /// <summary>Whether the last health check succeeded.</summary>
    public bool IsHealthy { get; private set; }

    /// <summary>Sanitized error message from the last failed check, if any.</summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>Number of consecutive failures since the last success.</summary>
    public int ConsecutiveFailures { get; private set; }

    // EF Core constructor
    private AdapterHealthRecord() { }

    /// <summary>Creates a new <see cref="AdapterHealthRecord"/> in a healthy initial state.</summary>
    public static AdapterHealthRecord Create(Guid tenantId, string adapterName, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(adapterName)) throw new ArgumentException("AdapterName required.", nameof(adapterName));

        return new AdapterHealthRecord
        {
            TenantId = tenantId,
            AdapterName = adapterName,
            LastCheckAt = clock.GetUtcNow(),
            IsHealthy = true,
            ConsecutiveFailures = 0
        };
    }

    /// <summary>
    /// Records a successful health check. Resets consecutive failure counter.
    /// Raises <see cref="AdapterHealthRecovered"/> if transitioning from unhealthy.
    /// </summary>
    public void RecordHealthy(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.GetUtcNow();
        var wasUnhealthy = !IsHealthy;

        LastCheckAt = now;
        LastSuccessAt = now;
        IsHealthy = true;
        LastErrorMessage = null;
        ConsecutiveFailures = 0;

        if (wasUnhealthy)
            RaiseDomainEvent(new AdapterHealthRecovered(TenantId, AdapterName, now));
    }

    /// <summary>
    /// Records a failed health check. Increments consecutive failure counter.
    /// Sanitizes the error message before storing (strips control chars, truncates).
    /// Always raises <see cref="AdapterHealthFailed"/>.
    /// </summary>
    public void RecordFailure(string errorMessage, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.GetUtcNow();
        var sanitized = AuditSanitizer.Sanitize(errorMessage);

        LastCheckAt = now;
        IsHealthy = false;
        LastErrorMessage = sanitized;
        ConsecutiveFailures++;

        RaiseDomainEvent(new AdapterHealthFailed(TenantId, AdapterName, sanitized, ConsecutiveFailures, now));
    }
}
