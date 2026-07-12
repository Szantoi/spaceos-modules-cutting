namespace SpaceOS.Modules.Cutting.Application.Services;

/// <summary>
/// Service for resolving tenant identity from subdomain-based hostnames.
/// Used by the public Customer Portal for multi-tenant routing.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves a tenant ID from a hostname containing a subdomain.
    /// </summary>
    /// <param name="hostname">
    /// The full hostname (e.g., "doorstar.joinerytech.hu").
    /// The first segment before the dot is extracted as the subdomain.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The unique identifier of the tenant matching the subdomain.</returns>
    /// <exception cref="TenantNotFoundException">
    /// Thrown when no tenant with the given subdomain exists.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the hostname format is invalid.
    /// </exception>
    Task<Guid> GetTenantIdFromSubdomain(string hostname, CancellationToken ct);
}

/// <summary>
/// Exception thrown when a tenant cannot be found for a given subdomain.
/// </summary>
public class TenantNotFoundException : Exception
{
    public TenantNotFoundException(string message) : base(message) { }
}
