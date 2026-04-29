using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Queries;

/// <summary>Handles <see cref="GetAdapterHealthQuery"/>.</summary>
public sealed class GetAdapterHealthQueryHandler : IRequestHandler<GetAdapterHealthQuery, Result<AdapterHealthDto>>
{
    private readonly IAdapterHealthRecordRepository _healthRepository;
    private readonly ITenantCuttingProviderConfigRepository _configRepository;

    public GetAdapterHealthQueryHandler(
        IAdapterHealthRecordRepository healthRepository,
        ITenantCuttingProviderConfigRepository configRepository)
    {
        ArgumentNullException.ThrowIfNull(healthRepository);
        ArgumentNullException.ThrowIfNull(configRepository);

        _healthRepository = healthRepository;
        _configRepository = configRepository;
    }

    /// <inheritdoc />
    public async Task<Result<AdapterHealthDto>> Handle(GetAdapterHealthQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var config = await _configRepository.GetByTenantAsync(request.TenantId, ct).ConfigureAwait(false);
        var adapterName = (config is { IsEnabled: true }) ? config.AdapterName : "builtin";

        var record = await _healthRepository.GetAsync(request.TenantId, adapterName, ct).ConfigureAwait(false);

        if (record is null)
        {
            // No health check has run yet — report as healthy (optimistic default)
            var now = DateTimeOffset.UtcNow;
            return Result<AdapterHealthDto>.Success(new AdapterHealthDto(
                request.TenantId,
                adapterName,
                IsHealthy: true,
                LastCheckAt: now,
                LastSuccessAt: null,
                ConsecutiveFailures: 0));
        }

        return Result<AdapterHealthDto>.Success(new AdapterHealthDto(
            record.TenantId,
            record.AdapterName,
            record.IsHealthy,
            record.LastCheckAt,
            record.LastSuccessAt,
            record.ConsecutiveFailures));
    }
}
