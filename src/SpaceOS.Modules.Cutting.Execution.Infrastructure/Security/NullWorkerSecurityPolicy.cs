using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Security;

/// <summary>
/// Stub implementation of IWorkerSecurityPolicy that always validates (DEV/TEST only).
/// TODO: Replace with real HMAC-based validation in production.
/// </summary>
internal sealed class NullWorkerSecurityPolicy : IWorkerSecurityPolicy
{
    public bool ValidateProgressEventHmac(Guid workerId, Guid executionId, Guid eventId, WorkerEventHmac hmac)
    {
        // Stub: always valid
        return true;
    }
}
