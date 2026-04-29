using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Commands;

/// <summary>
/// Creates or updates the cutting adapter configuration for a tenant.
/// On first call the config is created; on subsequent calls it is reconfigured with
/// optimistic concurrency check via <paramref name="ExpectedVersion"/>.
/// SEC-06: <paramref name="ConfigJson"/> is validated for plaintext secrets before saving.
/// </summary>
public sealed record ConfigureAdapterCommand(
    Guid TenantId,
    string AdapterName,
    string TransportName,
    string ConfigJson,
    short ConfigSchemaVersion,
    int ExpectedVersion,
    string? ChangeReason,
    Guid ActorId) : IRequest<Result>;
