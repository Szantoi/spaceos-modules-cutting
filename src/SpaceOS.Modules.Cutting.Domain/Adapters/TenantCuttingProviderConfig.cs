using Ardalis.Result;
using SpaceOS.Modules.Cutting.Domain.Adapters.Events;
using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Adapters;

/// <summary>
/// Aggregate root that stores a tenant's chosen external cutting adapter and transport configuration.
/// One record per tenant; versioned with optimistic concurrency control.
/// </summary>
public sealed class TenantCuttingProviderConfig : AggregateRoot
{
    private static readonly IReadOnlySet<string> AllowedAdapters =
        new HashSet<string>(StringComparer.Ordinal) { "builtin", "opticut", "cutrite", "manual" };

    private static readonly IReadOnlySet<string> AllowedTransports =
        new HashSet<string>(StringComparer.Ordinal) { "none", "file-exchange", "rest-api", "cli-wrapper" };

    /// <summary>Tenant identifier — used as primary key.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Name of the adapter implementation (e.g. "opticut", "builtin").</summary>
    public string AdapterName { get; private set; } = string.Empty;

    /// <summary>Name of the transport mechanism (e.g. "file-exchange", "none").</summary>
    public string TransportName { get; private set; } = string.Empty;

    /// <summary>Whether this adapter configuration is active.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>JSON-encoded adapter-specific configuration blob.</summary>
    public string ConfigJson { get; private set; } = string.Empty;

    /// <summary>Schema version of <see cref="ConfigJson"/> for forward-compat checking.</summary>
    public short ConfigSchemaVersion { get; private set; }

    /// <summary>Optimistic concurrency row version; incremented on every mutation.</summary>
    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }

    // EF Core constructor
    private TenantCuttingProviderConfig() { }

    /// <summary>
    /// Creates a new <see cref="TenantCuttingProviderConfig"/> for the given tenant.
    /// Raises <see cref="TenantAdapterConfigured"/>.
    /// </summary>
    public static Result<TenantCuttingProviderConfig> Create(
        Guid tenantId,
        string adapterName,
        string transportName,
        string configJson,
        short configSchemaVersion,
        Guid actorId,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (tenantId == Guid.Empty)
            return Result<TenantCuttingProviderConfig>.Invalid(
                new ValidationError("TenantId must not be empty."));

        if (actorId == Guid.Empty)
            return Result<TenantCuttingProviderConfig>.Invalid(
                new ValidationError("ActorId must not be empty."));

        var validation = ValidateAdapterAndTransport(adapterName, transportName);
        if (!validation.IsSuccess)
            return Result<TenantCuttingProviderConfig>.Invalid(validation.ValidationErrors.ToList());

        var now = clock.GetUtcNow();
        var config = new TenantCuttingProviderConfig
        {
            TenantId = tenantId,
            AdapterName = adapterName,
            TransportName = transportName,
            IsEnabled = true,
            ConfigJson = configJson ?? string.Empty,
            ConfigSchemaVersion = configSchemaVersion,
            Version = 1,
            CreatedAt = now,
            CreatedBy = actorId,
            UpdatedAt = now,
            UpdatedBy = actorId
        };

        config.RaiseDomainEvent(new TenantAdapterConfigured(tenantId, adapterName, transportName, actorId, now));
        return Result<TenantCuttingProviderConfig>.Success(config);
    }

    /// <summary>
    /// Updates the adapter/transport/config. Returns <see cref="Result.Conflict"/> when
    /// <paramref name="expectedVersion"/> does not match <see cref="Version"/>.
    /// Raises <see cref="TenantAdapterReconfigured"/>.
    /// </summary>
    public Result Reconfigure(
        string adapterName,
        string transportName,
        string configJson,
        short configSchemaVersion,
        int expectedVersion,
        Guid actorId,
        string? changeReason,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Version != expectedVersion)
            return Result.Conflict($"Version mismatch: expected {expectedVersion}, actual {Version}.");

        if (actorId == Guid.Empty)
            return Result.Invalid(new ValidationError("ActorId must not be empty."));

        var validation = ValidateAdapterAndTransport(adapterName, transportName);
        if (!validation.IsSuccess)
            return Result.Invalid(validation.ValidationErrors.ToList());

        AdapterName = adapterName;
        TransportName = transportName;
        ConfigJson = configJson ?? string.Empty;
        ConfigSchemaVersion = configSchemaVersion;
        IsEnabled = true;
        Version++;
        UpdatedAt = clock.GetUtcNow();
        UpdatedBy = actorId;

        RaiseDomainEvent(new TenantAdapterReconfigured(
            TenantId, adapterName, transportName, actorId, UpdatedAt, changeReason));

        return Result.Success();
    }

    /// <summary>
    /// Disables this adapter configuration. Operation is idempotent.
    /// Raises <see cref="TenantAdapterDisabled"/> only when transitioning from enabled.
    /// Returns <see cref="Result.Conflict"/> on version mismatch.
    /// </summary>
    public Result Disable(int expectedVersion, Guid actorId, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Version != expectedVersion)
            return Result.Conflict($"Version mismatch: expected {expectedVersion}, actual {Version}.");

        if (actorId == Guid.Empty)
            return Result.Invalid(new ValidationError("ActorId must not be empty."));

        if (!IsEnabled)
            return Result.Success(); // idempotent

        IsEnabled = false;
        Version++;
        UpdatedAt = clock.GetUtcNow();
        UpdatedBy = actorId;

        RaiseDomainEvent(new TenantAdapterDisabled(TenantId, actorId, UpdatedAt));
        return Result.Success();
    }

    private static Result ValidateAdapterAndTransport(string adapterName, string transportName)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(adapterName) || !AllowedAdapters.Contains(adapterName))
            errors.Add(new ValidationError(
                $"AdapterName '{adapterName}' is not allowed. Allowed: {string.Join(", ", AllowedAdapters)}."));

        if (string.IsNullOrWhiteSpace(transportName) || !AllowedTransports.Contains(transportName))
            errors.Add(new ValidationError(
                $"TransportName '{transportName}' is not allowed. Allowed: {string.Join(", ", AllowedTransports)}."));

        return errors.Count == 0 ? Result.Success() : Result.Invalid(errors);
    }
}
