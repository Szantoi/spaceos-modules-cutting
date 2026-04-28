using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.HashChain;

/// <summary>
/// No-op hash chain sink. Logs a warning when called.
/// Replace with a real Kernel hash-chain binding when the cross-module integration is available.
/// </summary>
public sealed class NullCuttingHashChainSink(ILogger<NullCuttingHashChainSink> logger) : ICuttingHashChainSink
{
    /// <inheritdoc />
    public Task AppendAsync(Guid tenantId, Guid executionId, string proofHash, CancellationToken ct)
    {
        logger.LogWarning(
            "NullCuttingHashChainSink: hash chain append skipped for execution {ExecutionId} tenant {TenantId}. Integrate with Kernel hash-chain.",
            executionId, tenantId);

        return Task.CompletedTask;
    }
}
