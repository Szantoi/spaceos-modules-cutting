using SpaceOS.Modules.Cutting.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>Factory that returns <see cref="ICuttingProvider"/> implementations by name.</summary>
public interface IAdapterFactory
{
    /// <summary>Returns the adapter registered under <paramref name="adapterName"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no adapter is registered with the given name.</exception>
    ICuttingProvider GetByName(string adapterName);

    /// <summary>The set of adapter names that have been registered.</summary>
    IReadOnlyCollection<string> RegisteredAdapterNames { get; }
}

/// <summary>Keyed registration that maps a string key to a provider instance.</summary>
public sealed record KeyedAdapterRegistration(string Key, ICuttingProvider Provider);
