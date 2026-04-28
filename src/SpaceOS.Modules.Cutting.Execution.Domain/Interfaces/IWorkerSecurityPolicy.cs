using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;

/// <summary>Port for validating worker-originated HMAC on progress events.</summary>
public interface IWorkerSecurityPolicy
{
    bool ValidateProgressEventHmac(Guid workerId, Guid executionId, Guid eventId, WorkerEventHmac hmac);
}
