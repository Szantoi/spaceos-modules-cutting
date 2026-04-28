using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// Resolves <see cref="ICuttingProvider"/> implementations by name.
/// Registered adapters are keyed by their string name (ordinal comparison).
/// </summary>
internal sealed class AdapterFactory : IAdapterFactory
{
    private readonly IReadOnlyDictionary<string, ICuttingProvider> _adapters;

    public AdapterFactory(IEnumerable<KeyedAdapterRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        _adapters = registrations
            .ToDictionary(r => r.Key, r => r.Provider, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public ICuttingProvider GetByName(string adapterName)
    {
        if (_adapters.TryGetValue(adapterName, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"No adapter registered with name '{adapterName}'. " +
            $"Registered: {string.Join(", ", _adapters.Keys)}.");
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> RegisteredAdapterNames => _adapters.Keys.ToArray();
}
