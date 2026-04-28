using SpaceOS.Modules.Cutting.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>
/// Resolves the active <see cref="ICuttingProvider"/> for the current tenant.
/// Applies caching and falls back to the builtin adapter when needed.
/// </summary>
public interface ICuttingProviderResolver
{
    /// <summary>
    /// Returns the provider configured for the current tenant.
    /// Returns the builtin provider when no config exists or the config is disabled.
    /// </summary>
    Task<ICuttingProvider> ResolveAsync(CancellationToken ct);
}
