using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Realtime;

/// <summary>
/// SignalR hub for real-time cutting execution updates.
/// Clients join a tenant-scoped group per execution to receive progress events.
/// </summary>
[Authorize(Policy = "ManufacturerOnly")]
public sealed class ExecutionHub : Hub
{
    /// <summary>
    /// Adds the caller to the group for the given execution.
    /// SEC-15: group name is prefixed with tenantId — callers can only join executions
    /// within their own tenant because the tenantId comes from their validated JWT claim.
    /// </summary>
    public async Task JoinExecution(Guid executionId)
    {
        var tenantId = ResolveTenantId(Context.User);

        if (tenantId is null)
        {
            Context.Abort();
            return;
        }

        var groupName = $"{tenantId.Value:D}:{executionId:D}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
    }

    /// <summary>Removes the caller from the execution group.</summary>
    public async Task LeaveExecution(Guid executionId)
    {
        var tenantId = ResolveTenantId(Context.User);

        if (tenantId is null)
            return;

        var groupName = $"{tenantId.Value:D}:{executionId:D}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
    }

    private static Guid? ResolveTenantId(ClaimsPrincipal? user)
    {
        var canonicalClaim = user?.FindFirst("tid");
        if (canonicalClaim is not null)
        {
            return Guid.TryParse(canonicalClaim.Value, out var canonicalTenantId)
                   && canonicalTenantId != Guid.Empty
                ? canonicalTenantId
                : null;
        }

        var legacyClaim = user?.FindFirst("tenant_id");
        return legacyClaim is not null
               && Guid.TryParse(legacyClaim.Value, out var legacyTenantId)
               && legacyTenantId != Guid.Empty
            ? legacyTenantId
            : null;
    }
}
