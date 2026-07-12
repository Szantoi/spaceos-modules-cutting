using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="ITenantResolver"/> that queries the Kernel's Tenants table
/// via raw SQL to resolve tenant identity from subdomain.
/// </summary>
public class TenantResolver : ITenantResolver
{
    private readonly IDbContextFactory<CuttingDbContext> _dbFactory;
    private readonly ILogger<TenantResolver> _logger;

    public TenantResolver(
        IDbContextFactory<CuttingDbContext> dbFactory,
        ILogger<TenantResolver> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Guid> GetTenantIdFromSubdomain(string hostname, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            throw new InvalidOperationException("Hostname cannot be null or empty.");

        // Extract subdomain from hostname (e.g., "doorstar.joinerytech.hu" → "doorstar")
        var subdomain = ExtractSubdomain(hostname);

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Query Kernel's Tenants table (cross-schema query)
        // Note: Tenants table is in the default 'public' schema (Kernel schema)
        var tenantId = await db.Database
            .SqlQueryRaw<Guid>(
                """
                SELECT "Id"
                FROM "Tenants"
                WHERE "Subdomain" = {0}
                LIMIT 1
                """,
                subdomain)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("No tenant found for subdomain: {Subdomain} (hostname: {Hostname})", subdomain, hostname);
            throw new TenantNotFoundException($"No tenant found for subdomain: {subdomain}");
        }

        _logger.LogInformation("Resolved tenant {TenantId} from subdomain: {Subdomain}", tenantId, subdomain);
        return tenantId;
    }

    private string ExtractSubdomain(string hostname)
    {
        // Split hostname by dots
        var parts = hostname.Split('.');

        if (parts.Length < 2)
            throw new InvalidOperationException($"Invalid hostname format: {hostname}. Expected at least 2 parts (subdomain.domain).");

        // Return the first part as the subdomain
        return parts[0].ToLowerInvariant();
    }
}
