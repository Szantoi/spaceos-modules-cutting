using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Queries;

/// <summary>Handles <see cref="GetAdapterConfigQuery"/>.</summary>
public sealed class GetAdapterConfigQueryHandler : IRequestHandler<GetAdapterConfigQuery, Result<AdapterConfigDto>>
{
    private readonly ITenantCuttingProviderConfigRepository _configRepository;

    public GetAdapterConfigQueryHandler(ITenantCuttingProviderConfigRepository configRepository)
    {
        ArgumentNullException.ThrowIfNull(configRepository);
        _configRepository = configRepository;
    }

    /// <inheritdoc />
    public async Task<Result<AdapterConfigDto>> Handle(GetAdapterConfigQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var config = await _configRepository.GetByTenantAsync(request.TenantId, ct).ConfigureAwait(false);

        if (config is null)
            return Result<AdapterConfigDto>.NotFound($"No adapter configuration found for tenant {request.TenantId}.");

        return Result<AdapterConfigDto>.Success(new AdapterConfigDto(
            config.TenantId,
            config.AdapterName,
            config.TransportName,
            config.IsEnabled,
            config.Version,
            config.UpdatedAt));
    }
}
