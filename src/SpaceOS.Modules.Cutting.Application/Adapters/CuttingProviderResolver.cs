using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>
/// Resolves the active <see cref="ICuttingProvider"/> for the current tenant using a 30-second
/// distributed cache. Falls back to the builtin adapter when no config exists or when disabled.
/// </summary>
public sealed class CuttingProviderResolver : ICuttingProviderResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IAdapterFactory _adapterFactory;
    private readonly ITenantCuttingProviderConfigRepository _configRepository;
    private readonly IDistributedCache _cache;
    private readonly IAdapterCallAuditWriter _auditWriter;
    private readonly TimeProvider _clock;
    private readonly ILogger<CuttingProviderResolver> _logger;

    // Tenant context is resolved from outside (caller sets tenantId)
    private readonly Func<Guid> _tenantIdResolver;

    public CuttingProviderResolver(
        IAdapterFactory adapterFactory,
        ITenantCuttingProviderConfigRepository configRepository,
        IDistributedCache cache,
        IAdapterCallAuditWriter auditWriter,
        TimeProvider clock,
        ILogger<CuttingProviderResolver> logger,
        Func<Guid> tenantIdResolver)
    {
        ArgumentNullException.ThrowIfNull(adapterFactory);
        ArgumentNullException.ThrowIfNull(configRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(auditWriter);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(tenantIdResolver);

        _adapterFactory = adapterFactory;
        _configRepository = configRepository;
        _cache = cache;
        _auditWriter = auditWriter;
        _clock = clock;
        _logger = logger;
        _tenantIdResolver = tenantIdResolver;
    }

    /// <inheritdoc />
    public async Task<ICuttingProvider> ResolveAsync(CancellationToken ct)
    {
        var tenantId = _tenantIdResolver();
        var cacheKey = $"adapter-config:{tenantId}";

        // Check cache first
        var cached = await _cache.GetStringAsync(cacheKey, ct).ConfigureAwait(false);
        string? adapterName = cached;

        if (adapterName is null)
        {
            // Cache miss — load from repository
            var config = await _configRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false);
            adapterName = (config is { IsEnabled: true }) ? config.AdapterName : "builtin";

            await _cache.SetStringAsync(
                cacheKey,
                adapterName,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                ct).ConfigureAwait(false);
        }

        if (!_adapterFactory.RegisteredAdapterNames.Contains(adapterName))
        {
            _logger.LogWarning(
                "Adapter '{AdapterName}' is configured for tenant {TenantId} but not registered. Falling back to builtin.",
                adapterName, tenantId);

            await _auditWriter.RecordCapabilityFallbackAsync(adapterName, "registration-missing", tenantId, ct)
                .ConfigureAwait(false);

            adapterName = "builtin";
        }

        return _adapterFactory.GetByName(adapterName);
    }
}
