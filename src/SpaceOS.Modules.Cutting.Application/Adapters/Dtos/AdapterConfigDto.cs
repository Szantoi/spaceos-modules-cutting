namespace SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

/// <summary>Read model for the tenant's current adapter configuration.</summary>
public sealed record AdapterConfigDto(
    Guid TenantId,
    string AdapterName,
    string TransportName,
    bool IsEnabled,
    int Version,
    DateTimeOffset ConfiguredAt);

/// <summary>Result of an adapter connectivity test.</summary>
public sealed record AdapterTestResultDto(bool IsHealthy, string Message);

/// <summary>Read model for the adapter health state of a given tenant.</summary>
public sealed record AdapterHealthDto(
    Guid TenantId,
    string AdapterName,
    bool IsHealthy,
    DateTimeOffset LastCheckAt,
    DateTimeOffset? LastSuccessAt,
    int ConsecutiveFailures);
