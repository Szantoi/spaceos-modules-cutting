using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Api.Endpoints;

public static class InternalEndpoints
{
    private const string InternalHeader = "X-SpaceOS-Internal";
    private const string AllowlistEnvKey = "TEST_TENANT_ALLOWLIST";

    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/internal/cutting-sheets/by-tenant/{tenantId}", DeleteByTenant)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> DeleteByTenant(
        string tenantId,
        string? confirm,
        HttpRequest request,
        ICuttingRepository repo,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        // 1. X-SpaceOS-Internal header required
        if (!request.Headers.TryGetValue(InternalHeader, out var headerVal) ||
            headerVal != "true")
            return Results.StatusCode(403);

        // 2. ?confirm=true required
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "confirm=true query parameter is required." });

        // 3. GUID format validation
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return Results.BadRequest(new { error = "tenantId must be a valid GUID." });

        // 4. Allowlist check
        var allowlist = Environment.GetEnvironmentVariable(AllowlistEnvKey) ?? string.Empty;
        var allowed = allowlist
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToHashSet();

        if (!allowed.Contains(tenantGuid))
        {
            logger.LogWarning(
                "DeleteByTenant rejected: tenant {TenantId} not in TEST_TENANT_ALLOWLIST", tenantGuid);
            return Results.StatusCode(403);
        }

        var (sheets, plans) = await repo.DeleteByTenantAsync(tenantGuid, ct).ConfigureAwait(false);

        return Results.Ok(new
        {
            tenantId = tenantGuid,
            deletedCounts = new { cuttingSheets = sheets, dailyCuttingPlans = plans }
        });
    }
}
