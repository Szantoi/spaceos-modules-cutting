using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
    public async Task JoinExecution(string executionId)
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value
                    ?? Context.User?.FindFirst("tid")?.Value;

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            Context.Abort();
            return;
        }

        var groupName = $"{tenantId}:{executionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
    }

    /// <summary>Removes the caller from the execution group.</summary>
    public async Task LeaveExecution(string executionId)
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value
                    ?? Context.User?.FindFirst("tid")?.Value;

        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        var groupName = $"{tenantId}:{executionId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
    }
}
