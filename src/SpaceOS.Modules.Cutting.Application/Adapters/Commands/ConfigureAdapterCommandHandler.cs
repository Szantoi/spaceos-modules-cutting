using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using SpaceOS.Modules.Cutting.Domain.Adapters;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Commands;

/// <summary>
/// Handles <see cref="ConfigureAdapterCommand"/>.
/// Creates a new config or reconfigures an existing one with optimistic concurrency.
/// SEC-06: validates ConfigJson for plaintext secrets before persisting.
/// </summary>
public sealed class ConfigureAdapterCommandHandler : IRequestHandler<ConfigureAdapterCommand, Result>
{
    private readonly ITenantCuttingProviderConfigRepository _configRepository;
    private readonly IConfigSecretDetector _secretDetector;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _clock;

    public ConfigureAdapterCommandHandler(
        ITenantCuttingProviderConfigRepository configRepository,
        IConfigSecretDetector secretDetector,
        IDistributedCache cache,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(configRepository);
        ArgumentNullException.ThrowIfNull(secretDetector);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(clock);

        _configRepository = configRepository;
        _secretDetector = secretDetector;
        _cache = cache;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ConfigureAdapterCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // SEC-06: reject plaintext secrets before touching the database
        var secretCheck = _secretDetector.ValidateConfigJson(request.ConfigJson);
        if (!secretCheck.IsSuccess)
            return secretCheck;

        var existing = await _configRepository.GetByTenantAsync(request.TenantId, ct).ConfigureAwait(false);

        if (existing is null)
        {
            // First-time configuration for this tenant
            var createResult = TenantCuttingProviderConfig.Create(
                request.TenantId,
                request.AdapterName,
                request.TransportName,
                request.ConfigJson,
                request.ConfigSchemaVersion,
                request.ActorId,
                _clock);

            if (!createResult.IsSuccess)
                return Result.Invalid(createResult.ValidationErrors.ToList());

            createResult.Value.PopDomainEvents();
            await _configRepository.AddAsync(createResult.Value, ct).ConfigureAwait(false);
        }
        else
        {
            // Reconfigure — optimistic concurrency via ExpectedVersion
            var reconfigureResult = existing.Reconfigure(
                request.AdapterName,
                request.TransportName,
                request.ConfigJson,
                request.ConfigSchemaVersion,
                request.ExpectedVersion,
                request.ActorId,
                request.ChangeReason,
                _clock);

            if (!reconfigureResult.IsSuccess)
                return reconfigureResult;

            existing.PopDomainEvents();
            await _configRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
        }

        // Invalidate resolver cache so the next call picks up the new configuration
        var cacheKey = $"adapter-config:{request.TenantId}";
        await _cache.RemoveAsync(cacheKey, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
